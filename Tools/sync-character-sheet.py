"""Synchronize the approved character worksheet before the Unity build."""

import argparse
import hashlib
import io
import json
import os
from pathlib import Path
import posixpath
import re
import struct
import tempfile
from xml.etree import ElementTree as XML
from xml.sax.saxutils import escape
from zipfile import ZIP_DEFLATED, ZipFile

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets/DragonIdle"
WORKBOOK = ROOT / "outputs/character/character.xlsx"
BINDINGS = ROOT / "Tools/round-character-assets.json"
CATALOG = ASSETS / "Core/RoundSheetCatalog.cs"
KITS = ASSETS / "Resources/Config/Characters/RoundKits.json"
PORTRAITS = ASSETS / "Resources/Config/Characters/RoundPortraits.json"
N = {
    "s": "http://schemas.openxmlformats.org/spreadsheetml/2006/main",
    "pr": "http://schemas.openxmlformats.org/package/2006/relationships",
    "r": "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
    "xdr": "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing",
    "a": "http://schemas.openxmlformats.org/drawingml/2006/main",
}
STATES = {"站立": 1, "击退": 2, "小浮空": 3, "大浮空": 4, "倒地": 5}
TYPES = {"输出": "Output", "防御": "Defense", "援助": "Support"}
VALUE = r"\[([1-9]\d*)\]"
COUNT = r"(?:\{|\[)?([1-9]\d*)(?:\}|\])?"
REACTION = r"\{?(" + "|".join(STATES) + r")\}?"
TAG = r"【([^】]+)】"
CHAIN_STATES = ("小浮空", "大浮空", "倒地", "击退")
TAGS = TAG + r"(?:、" + TAG + r")*"


def sheet_text(value):
    return re.sub(r"\s+", "", value).translate(str.maketrans({",": "，", ";": "，", "；": "，"}))


def require(pattern, value, address):
    match = re.fullmatch(pattern, sheet_text(value))
    if not match:
        raise ValueError(f"{address} 的技能表述需要补充运行规则：{value}")
    return match


def power(value, limit=400):
    n = int(value)
    if not 0 < n <= limit:
        raise ValueError(f"倍率超出范围：{n}")
    return n / 100


def duration(value):
    return int(value) if value else -1


def compile_moves(cells, row):
    basic, ultimate, pursuit = (cells[f"E{row+n}"] for n in (7, 8, 9))
    moves = {}
    if sheet_text(basic).startswith("为自身回复"):
        hp = require("为自身回复" + VALUE, basic, f"E{row+7}")
        bp = power(hp[1])
        moves["basicHealsSelf"] = True
    elif re.search(r"概率造成\[", sheet_text(basic)):
        m = require(r"对(正面|随机)敌方造成" + VALUE + r"伤害，(一定概率|高概率|极高概率)造成" + VALUE + "伤害", basic, f"E{row+7}")
        bp = power(m[2])
        moves.update(basicRandomTarget=m[1]=="随机", basicEnhancedPower=power(m[4]),
                     basicEnhancedChance={"一定概率": .30, "高概率": .60, "极高概率": .90}[m[3]])
    else:
        m = require(r"对(正面|随机)敌方造成" + VALUE + r"伤害(?:，(一定概率|高概率|极高概率)造成" + REACTION + r")?", basic, f"E{row+7}")
        bp = power(m[2])
        moves["basicRandomTarget"] = m[1] == "随机"
        if m[3]:
            moves["basicReactionChance"] = {"一定概率": .30, "高概率": .60, "极高概率": .90}[m[3]]
            moves["basicReaction"] = STATES[m[4]]

    note = cells.get(f"K{row+8}", "")
    ub = sheet_text(ultimate)
    if ub.startswith("进入【蓄力】"):
        m = require(r"进入【蓄力】状态(一|1)回合，随后对生命最低敌方造成" + VALUE + "伤害", ultimate, f"E{row+8}")
        up = power(m[2])
        damage = require(r"【蓄力】期间无法攻击，伤害减免" + VALUE, note, f"K{row+8}")
        moves.update(ultimateChargeRounds=1, ultimateChargeReduction=power(damage[1], 100))
    elif ub.startswith("对随机两名"):
        m = require(r"对随机两名敌方造成" + VALUE + r"伤害，提升自身" + VALUE + "攻击" + COUNT + "回合",
                    ultimate, f"E{row+8}")
        up = power(m[1])
        moves.update(ultimateTargets=2, ultimateAttackBonus=power(m[2], 100), ultimateAttackRounds=duration(m[3]))
    elif ub.startswith("对随机至多"):
        m = require(r"对随机至多(三|六|[1-6])名(?:随机)?敌方造成" + VALUE
                    + r"伤害，造成(?:\{([1-6])连击\}|([1-6])连击)", ultimate, f"E{row+8}")
        count = {"三": 3, "六": 6}.get(m[1]) if not m[1].isdigit() else int(m[1])
        if count != int(m[3] or m[4]):
            raise ValueError(f"E{row+8} 的目标数与命中数不一致")
        up = power(m[2])
        moves["ultimateHits"] = count
        if count == 3:
            moves["ultimateDistinctVolley"] = True
    elif ub.startswith("损失"):
        m = require(r"损失" + VALUE + r"血量，对正面横排敌方造成" + VALUE + r"伤害", ultimate, f"E{row+8}")
        up = power(m[2])
        moves.update(ultimateHealthCost=power(m[1], 100), ultimateFrontRow=True)
    elif ub.startswith("对防御最低"):
        m = require(r"对防御最低的?敌方造成" + VALUE + "伤害", ultimate, f"E{row+8}")
        up = power(m[1])
        moves["ultimateLowestDefense"] = True
    elif ub.startswith(("对正面横排", "对正面一列", "对攻击最高")):
        m = require(r"对(正面横排|正面一列|攻击最高的?敌方|攻击最高地方)造成" + VALUE
                    + r"伤害(?:，随机目标造成" + REACTION + r"|，降低目标" + VALUE + r"防御(?:，?(?:持续)?" + COUNT + r"回合)?)?",
                    ultimate, f"E{row+8}")
        moves[{"正面横排": "ultimateFrontRow", "正面一列": "ultimateFrontColumn"}.get(m[1], "ultimateHighestAttack")] = True
        up = power(m[2])
        if m[3]:
            moves["ultimateReaction"] = STATES[m[3]]
        if m[4]:
            moves.update(ultimateDefenseReduction=power(m[4], 100), ultimateDefenseRounds=duration(m[5]))
    else:
        m = require(r"对正面敌方造成" + VALUE + "伤害", ultimate, f"E{row+8}")
        up = power(m[1])

    if re.match(r"追打\{?\d", sheet_text(pursuit)):
        m = require(r"追打(?:\{(\d+)连击\}|\{(\d+)\}连击|(\d+)连击)，对随机(?:单位|敌方)造成" + VALUE
                    + r"伤害和(?:\{(\d+)连击\}|\{(\d+)\}连击|(\d+)连击)，自身提升" + VALUE
                    + r"闪避(?:，?持续" + COUNT + r"回合)?", pursuit, f"E{row+9}")
        pp = power(m[4])
        moves.update(pursuitCombo=int(m[1] or m[2] or m[3]), pursuitHits=int(m[5] or m[6] or m[7]),
                     pursuitLimit=1, pursuitDodgeIncrease=power(m[8], 100), pursuitDodgeRounds=duration(m[9]))
    elif sheet_text(pursuit).startswith("追打【"):
        m = require("追打" + TAG + "角色造成的追打状态，造成" + VALUE + "伤害和" + REACTION, pursuit, f"E{row+9}")
        pp = power(m[2])
        moves.update(pursuitSourceTag=m[1], pursuitReaction=STATES[m[3]], pursuitLimit=1)
    else:
        m = require("追打" + REACTION + r"，造成" + VALUE + "伤害和" + REACTION
                    + r"(?:，每回合至多" + COUNT + r"次)?(?:，提升" + VALUE + "防御" + COUNT + "回合)?", pursuit, f"E{row+9}")
        pp = power(m[2])
        moves.update(pursuitTrigger=STATES[m[1]], pursuitReaction=STATES[m[3]], pursuitLimit=int(m[4] or 1))
        if m[5]:
            moves.update(pursuitDefenseIncrease=power(m[5], 100), pursuitDefenseRounds=duration(m[6]))
    if note and not moves.get("ultimateChargeRounds"):
        raise ValueError(f"K{row+8} 的额外技能规则需要编译：{note}")
    moves.update(basicText=basic, ultimateText=ultimate, pursuitText=pursuit)
    if note:
        moves["ultimateNote"] = note
    return bp, up, pp, moves


def combo_field(cells, row, default, capacity=10):
    address = f"N{row}"
    raw = cells.get(address)
    if raw is None:
        return default
    if not isinstance(raw, str) or not re.fullmatch(r"(?:0|[1-9]|10)", raw):
        raise ValueError(f"{address} 的技能连击数须为 0 至 10 的整数")
    value = int(raw)
    if (value == 0) != (default == 0) or value > capacity:
        raise ValueError(f"{address} 的技能连击数与招式实际攻击次数不符")
    return value


def move_combo_defaults(techniques):
    ultimate = max(1, techniques.get("ultimateTargets", 0), techniques.get("ultimateHits", 0))
    if techniques.get("ultimateFrontRow") or techniques.get("ultimateFrontColumn"):
        ultimate = 3
    return (0 if techniques.get("basicHealsSelf") else 1, ultimate, techniques.get("pursuitHits", 1))


def skill_combo_default(skill):
    return int(skill["effect"] in ("strike", "counter", "detonate"))


def compile_skill(name, value, kind, address):
    text = sheet_text(value)
    if kind == "被动" and text.startswith("攻击提升"):
        m = require(r"攻击提升" + VALUE + r"，(?:己方编队" + TAG + "类别角色时，防御提升" + VALUE
                    + r"|对(" + TAGS + r")(?:角色)?伤害减免" + VALUE + ")", value, address)
        skill = dict(trigger="opening", effect="attack", value=power(m[1], 100))
        if m[2]:
            skill.update(allyTag=m[2], allyDefense=power(m[3], 100))
        else:
            skill.update(resistTags=re.findall(TAG, m[4]), finalReduction=power(m[7], 100))
    elif kind == "被动" and text.startswith("自身存活时"):
        m = require(r"自身存活时，己方(" + TAGS + r")角色提升" + VALUE + "攻击", value, address)
        skill = dict(trigger="whileAlive", effect="teamAttack", teamAttackTags=re.findall(TAG, m[1]), value=power(m[4], 100))
    elif kind == "被动" and text.startswith("前"):
        m = require(r"前" + COUNT + r"回合，提升" + VALUE + r"攻击[，。]" + COUNT + r"回合开始，降低"
                    + VALUE + "攻击，提升" + VALUE + "防御", value, address)
        if int(m[1]) + 1 != int(m[3]):
            raise ValueError(f"{address} 的前后阶段必须相邻")
        skill = dict(trigger="roundStart", effect="phaseStats", value=power(m[2], 100), phaseRound=int(m[3]),
                     lateAttackPenalty=power(m[4], 100), lateDefense=power(m[5], 100))
    elif kind in ("触发", "行动") and text.startswith("被单体技能攻击时"):
        m = require(r"被单体技能攻击时，闪避攻击并对敌方造成" + VALUE + "伤害和" + REACTION + r"，每回合至多(?:一|1)次", value, address)
        skill = dict(trigger="onSingleTarget", effect="counter", value=power(m[1], 100), reaction=STATES[m[2]], usesPerRound=1)
    elif kind in ("触发", "行动") and text.startswith("回合开始时"):
        m = require(r"回合开始时触发，对随机敌人造成" + VALUE + "伤害", value, address)
        skill = dict(trigger="onRoundStart", effect="strike", value=power(m[1]))
    elif kind in ("触发", "行动") and text.startswith("阵亡前触发"):
        m = require(r"阵亡前触发，对击杀自身的敌人造成" + VALUE + r"伤害，己方("
                    + TAGS + r")提升" + VALUE + "攻击", value, address)
        skill = dict(trigger="onDefeat", effect="detonate", value=power(m[1]),
                     teamAttackTags=re.findall(TAG, m[2]), defeatAttackBonus=power(m[5], 100))
    elif kind in ("触发", "行动") and text.startswith("阵亡时"):
        m = require(r"阵亡时以" + VALUE + r"生命复活，复活后己方(" + TAGS + r")提升" + VALUE + "攻击", value, address)
        skill = dict(trigger="onDefeat", effect="revive", value=power(m[1], 100),
                     teamAttackTags=re.findall(TAG, m[2]), reviveAttackBonus=power(m[5], 100))
    elif kind in ("触发", "行动") and text.startswith("受到攻击时"):
        m = require(r"受到攻击时，(一定概率|高概率|极高概率)伤害减免" + VALUE, value, address)
        skill = dict(trigger="onReceive", effect="mitigate", value=power(m[2], 100),
                     chance={"一定概率": .30, "高概率": .60, "极高概率": .90}[m[1]])
    else:
        raise ValueError(f"{address} 的技能类型或效果需要补充运行规则：{kind} / {value}")
    return dict(name=name, text=value, **skill)


def resolve_part(part, target):
    result = posixpath.normpath(posixpath.join(posixpath.dirname(part), target)) if not target.startswith("/") else target[1:]
    if not result.startswith("xl/"):
        raise ValueError(f"工作簿内部资源路径无效：{target}")
    return result


def rels_part(part):
    directory, name = posixpath.split(part)
    return posixpath.join(directory, "_rels", name + ".rels")


def relationship(xml, id):
    matches = [entry for entry in xml if entry.get("Id") == id and entry.get("TargetMode") != "External"]
    if len(matches) != 1:
        raise ValueError(f"工作簿缺少有效内部引用：{id}")
    return matches[0]


def load_sheet(archive):
    work = "xl/workbook.xml"
    sheet = XML.fromstring(archive.read(work)).find("./s:sheets/s:sheet[@name='角色设计']", N)
    if sheet is None:
        raise ValueError("未找到「角色设计」工作表")
    rel = relationship(XML.fromstring(archive.read(rels_part(work))), sheet.get(f"{{{N['r']}}}id"))
    path = resolve_part(work, rel.get("Target"))
    sheet = XML.fromstring(archive.read(path))
    strings = []
    if "xl/sharedStrings.xml" in archive.namelist():
        strings = ["".join(node.text or "" for node in item.findall(".//s:t", N))
                   for item in XML.fromstring(archive.read("xl/sharedStrings.xml"))]
    cells = {}
    for cell in sheet.findall("./s:sheetData/s:row/s:c", N):
        value = cell.findtext("s:v", namespaces=N)
        kind = cell.get("t")
        address = cell.get("r")
        if kind == "s":
            value = strings[int(value)]
        elif kind == "inlineStr":
            value = "".join(node.text or "" for node in cell.findall(".//s:t", N))
        elif kind == "b":
            value = value == "1"
        if value not in (None, ""):
            cells[address] = value
    return path, sheet, cells


def chain_statistics(cells):
    rows = sorted((int(address[1:]) for address, value in cells.items()
                   if address.startswith("G") and value == "角色编号"),
                  key=lambda row: int(cells[f"M{row}"]))
    produced = dict.fromkeys(CHAIN_STATES, 0)
    followed = dict.fromkeys(CHAIN_STATES, 0)
    updates = {}
    for row in rows:
        for offset in range(7, 13):
            text = sheet_text(cells.get(f"E{row+offset}", ""))
            if not text:
                continue
            lead, separator, effect = text.partition("，")
            if lead.startswith("追打"):
                for state in CHAIN_STATES:
                    if state in lead or "角色造成的追打状态" in lead:
                        followed[state] += 1
                text = effect if separator else ""
            for state in CHAIN_STATES:
                if re.search(r"(?:造成|伤害和)\{?" + state + r"\}?", text):
                    produced[state] += 1
        for offset, values in enumerate((
                ("追打状态累计", "截至本编号"), ("技能数", *CHAIN_STATES),
                ("造成", *(str(produced[state]) for state in CHAIN_STATES)),
                ("衔接", *(str(followed[state]) for state in CHAIN_STATES)))):
            updates.update({f"{column}{row+offset}": value for column, value in zip("QRSTU", values)})
    return updates


def update_sheet_cells(xml, updates):
    # WPS-specific worksheet metadata and drawings stay byte-identical outside these cells.
    source = xml.decode("utf-8")
    for address, value in updates.items():
        row = int(re.search(r"\d+", address)[0])
        row_pattern = re.compile(r'(<row\b[^>]*\br="' + str(row) + r'"[^>]*>)(.*?)(</row>)', re.S)
        match = row_pattern.search(source)
        if match is None:
            raise ValueError(f"角色设计缺少第 {row} 行")
        body = match[2]
        cell_pattern = re.compile(r'<c\b[^>]*\br="' + address + r'"(?:[^>]*?/>|[^>]*>.*?</c>)', re.S)
        prior = cell_pattern.search(body)
        style = re.search(r'\bs="\d+"', prior[0].split(">")[0]) if prior else None
        if not style:
            style = re.search(r'<c\b[^>]*\br="C' + str(row) + r'"[^>]*?\b(s="\d+")', body)
        attrs = f'r="{address}"' + (f' {style[1] if style.lastindex else style[0]}' if style else "")
        cell = (f'<c {attrs}><v>{value}</v></c>' if value.isdecimal()
                else f'<c {attrs} t="inlineStr"><is><t>{escape(value)}</t></is></c>')
        if prior:
            body = body[:prior.start()] + cell + body[prior.end():]
        elif address[0] in "QRSTU":
            body += cell
        else:
            raise ValueError(f"角色设计缺少预留单元格 {address}")
        source = source[:match.start(2)] + body + source[match.end(2):]
    if any(address[0] in "QRSTU" for address in updates):
        source = re.sub(r'(<dimension ref="[A-Z]+\d+:)P(\d+")', r'\g<1>U\2', source, count=1)
        if not re.search(r'<col min="17"', source):
            source = source.replace("</cols>", '<col min="17" max="17" width="20" customWidth="1"/>'
                                    '<col min="18" max="21" width="14" customWidth="1"/></cols>', 1)
    return source.encode("utf-8")


def render_catalog(cards, featured):
    def quote(value):
        return json.dumps(value, ensure_ascii=False)
    entries = "\n".join(
        '   { '+quote(c["id"])+', new RoundSheetCard('+', '.join(
            [quote(c["name"]), quote(c["title"]), quote(c["rarity"]), "CharacterType."+c["type"], str(c["number"]),
             'new[]{'+', '.join(map(quote, c["tags"]))+'}',
             *map(quote,c["potentials"])])+') },'
        for c in cards
    )
    ids = ", ".join(quote(c["id"]) for c in cards)
    return f"""// Generated by Tools/sync-character-sheet.py from the character workbook.
using System;
using System.Collections.Generic;

namespace DragonIdle {{
 internal sealed class RoundSheetCard {{
  internal readonly string Name,Title,Rarity,AttackPotential,DefensePotential,HpPotential;
  internal readonly CharacterType Type;
  internal readonly int Number;
  internal readonly string[] Tags;

  internal RoundSheetCard(string name,string title,string rarity,CharacterType type,int number,string[] tags,string attackPotential,string defensePotential,string hpPotential){{
   Name=name;Title=title;Rarity=rarity;Type=type;Number=number;Tags=tags;
   AttackPotential=attackPotential;DefensePotential=defensePotential;HpPotential=hpPotential;
  }}
 }}
 internal static class RoundSheetCatalog {{
  internal const string Featured={quote(featured)};
  internal static readonly System.Collections.ObjectModel.ReadOnlyCollection<string> Online=
   Array.AsReadOnly(new[]{{{ids}}});
  internal static readonly Dictionary<string,RoundSheetCard> Cards=new Dictionary<string,RoundSheetCard>{{
{entries}
  }};
 }}
}}
"""


def parse_cards(cells, bindings):
    chars, kits, seen = [], [], set()
    for address, value in cells.items():
        if not address.startswith("G") or value != "角色编号":
            continue
        row = int(address[1:])
        design_id = cells[f"H{row}"]
        prefix = re.match(r"\d{4}", design_id)
        if not prefix or prefix[0] in seen:
            raise ValueError(f"H{row} 的角色编号为空、重复或无效")
        seen.add(prefix[0])
        online = cells.get(f"C{row+4}", "")
        if online in ("", "否"):
            continue
        if online != "是" or not {"cardId", "portrait", "moves", "skills"} <= set(bindings.get(prefix[0], {})):
            raise ValueError(f"H{row} 的上线角色缺少已接入的稳定 ID 与素材绑定：{design_id}")
        bind = bindings[prefix[0]]
        id, portrait = bind["cardId"], bind["portrait"]
        png = ASSETS / f"Resources/Art/{portrait}.png"
        number = int(cells[f"M{row}"])
        name, card_type = [cells[f"C{row+n}"] for n in (1, 5)]
        rarity = cells.get(f"C{row+3}", bind.get("defaultRarity", ""))
        title = cells.get(f"C{row+2}", bind.get("defaultTitle"))
        if not title:
            raise ValueError(f"C{row+2} 缺少时期描述")
        tags = [cells[f"{c}{row+1}"] for c in "DEFGHIJKL" if cells.get(f"{c}{row+2}") is True]
        if number <= 0 or rarity not in "CBAS" or len(rarity) != 1 or card_type not in TYPES or "赛亚人篇" not in tags:
            raise ValueError(f"H{row} 的序号、稀有度、类型或身份无效")
        if not png.is_file():
            raise ValueError(f"H{row} 缺少运行立绘：{portrait}")
        names = [cells[f"C{row+n}"] for n in (7, 8, 9)]
        bp, up, pp, techniques = compile_moves(cells, row)
        for offset, (key, count) in enumerate(zip(("basicComboHits", "ultimateComboHits", "pursuitComboHits"),
                                                move_combo_defaults(techniques)), 7):
            techniques[key] = combo_field(cells, row+offset, count, capacity=count if count > 1 else 10)
        skills = []
        for index, offset in enumerate((10, 11, 12)):
            name_cell = cells.get(f"C{row+offset}", "")
            if name_cell in ("", "-", "[-]"):
                continue
            skill = compile_skill(name_cell, cells[f"E{row+offset}"], cells[f"D{row+offset}"], f"E{row+offset}")
            skill["comboHits"] = combo_field(cells, row+offset, skill_combo_default(skill))
            skill["icon"] = bind["skills"][index]
            skills.append(skill)
        max_skills = 1 if rarity == "C" else 2
        if not 1 <= len(skills) <= max_skills:
            raise ValueError(f"H{row} 的正式技能数量与稀有度不符")
        if len(skills) < max_skills:
            skills.append(dict(name="技能 2 待补充", trigger="reserved", effect="none", value=0))
        kit = dict(cardId=id, basic=names[0], ultimate=names[1], pursuit=names[2], basicPower=bp, ultimatePower=up,
                   pursuitPower=pp, techniques=techniques, skills=skills, **bind["moves"])
        if any(not (ASSETS / ("Resources/"+icon["resource"]+".png")).is_file()
               for icon in [*bind["moves"].values(), *(s["icon"] for s in skills if "icon" in s)]):
            raise ValueError(f"H{row} 的正式招式缺少运行图标")
        potentials = [cells.get(f"{col}{row+2}", "B") for col in "NOP"]
        if any(grade not in ("C", "B", "A", "S") for grade in potentials):
            raise ValueError(f"N{row+2}:P{row+2} 的属性潜力须为 C、B、A 或 S")
        chars.append(dict(row=row, id=id, name=name, title=title, rarity=rarity, type=TYPES[card_type],
                          tags=tags, number=number, portrait=portrait, png=png, potentials=potentials))
        kits.append((number, kit))
    if not chars or len(chars) != len({c["number"] for c in chars}) or len(chars) != len({c["id"] for c in chars}):
        raise ValueError("当前上线角色为空，或序号及稳定 ID 重复")
    return sorted(chars, key=lambda c: c["number"]), [kit for _, kit in sorted(kits, key=lambda item: item[0])]


def portrait_anchor(row, index, embed, image, size=None):
    width, height = struct.unpack(">II", image[16:24])
    scale = min(192 / width, 288 / height) * 9525
    cx, cy = size or (int(width*scale), int(height*scale))
    return XML.fromstring(f"""<xdr:oneCellAnchor xmlns:xdr="{N['xdr']}" xmlns:a="{N['a']}" xmlns:r="{N['r']}">
<xdr:from><xdr:col>0</xdr:col><xdr:colOff>114300</xdr:colOff><xdr:row>{row}</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:from>
<xdr:ext cx="{cx}" cy="{cy}"/><xdr:pic><xdr:nvPicPr><xdr:cNvPr id="{index}" name="{embed}"/><xdr:cNvPicPr><a:picLocks noChangeAspect="1"/></xdr:cNvPicPr></xdr:nvPicPr>
<xdr:blipFill><a:blip r:embed="{embed}"/><a:stretch><a:fillRect/></a:stretch></xdr:blipFill><xdr:spPr>
<a:xfrm><a:off x="0" y="0"/><a:ext cx="{cx}" cy="{cy}"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></xdr:spPr></xdr:pic><xdr:clientData/></xdr:oneCellAnchor>""")


def portraits(archive, sheet_path, sheet, cards):
    drawing = sheet.find("s:drawing", N)
    if drawing is None:
        raise ValueError("「角色设计」工作表缺少立绘区域的绘图关系")
    sheet_rel = XML.fromstring(archive.read(rels_part(sheet_path)))
    part = resolve_part(sheet_path, relationship(sheet_rel, drawing.get(f"{{{N['r']}}}id")).get("Target"))
    rel_path = rels_part(part)
    draw, rels = XML.fromstring(archive.read(part)), XML.fromstring(archive.read(rel_path))
    entries = {}
    counts = {node.get("Id") for node in rels}
    picture_id = max((int(node.get("id")) for node in draw.findall(".//xdr:cNvPr", N)), default=0)
    embed_attr = f"{{{N['r']}}}embed"
    changed = 0
    for card in cards:
        image = card["png"].read_bytes()
        start = card["row"]
        found = [node for node in draw if node.find(".//xdr:pic", N) is not None and node.findtext("xdr:from/xdr:col", namespaces=N) == "0"
                 and start <= int(node.findtext("xdr:from/xdr:row", namespaces=N)) <= start+10]
        if len(found) > 1:
            raise ValueError(f"A{start} 的卡片区有多张图片，无法判断应替换哪张立绘")
        new_part = f"xl/media/dragon-{card['id']}.png"
        if found:
            blip = found[0].find(".//a:blip", N)
            old_rel = relationship(rels, blip.get(embed_attr))
            old_part = resolve_part(part, old_rel.get("Target"))
            if archive.read(old_part) == image:
                continue
            if old_part == new_part:
                entries[new_part] = image
                changed += 1
                continue
        embed = "rIdDragon" + str(card["number"])
        if embed in counts:
            raise ValueError(f"A{start} 的立绘资源标识重复")
        counts.add(embed)
        XML.SubElement(rels, f"{{{N['pr']}}}Relationship", Id=embed, Type=N["r"]+"/image",
                       Target=posixpath.relpath(new_part, posixpath.dirname(part)))
        if found:
            blip.set(embed_attr, embed)
        else:
            picture_id += 1
            draw.append(portrait_anchor(start+2, picture_id, embed, image))
        entries[new_part] = image
        changed += 1
    if changed:
        for prefix in ("xdr", "a", "r"):
            XML.register_namespace(prefix, N[prefix])
        XML.register_namespace("", N["pr"])
        entries[part] = XML.tostring(draw, encoding="utf-8", xml_declaration=True)
        entries[rel_path] = XML.tostring(rels, encoding="utf-8", xml_declaration=True)
    return entries, changed


def digest(data):
    return hashlib.sha256(data).hexdigest()


def atomic_write(path, payload):
    with tempfile.NamedTemporaryFile(dir=path.parent, delete=False) as stream:
        tmp = Path(stream.name)
        stream.write(payload)
    try:
        os.replace(tmp, path)
    finally:
        if tmp.exists():
            tmp.unlink()


def sheet_sync(workbook, check):
    origin = workbook.read_bytes()
    old_kits = json.loads(KITS.read_text(encoding="utf-8-sig"))
    with ZipFile(io.BytesIO(origin)) as archive:
        path, sheet, cells = load_sheet(archive)
        cards, kits = parse_cards(cells, json.loads(BINDINGS.read_text(encoding="utf-8-sig")))
        old_ids = {kit["cardId"] for kit in old_kits["cards"]}
        old_catalog = CATALOG.read_text(encoding="utf-8") if CATALOG.exists() else ""
        prior_featured = re.search(r'Featured="([^"]+)"', old_catalog)
        newcomers = [c["id"] for c in cards if c["id"] not in old_ids]
        active = {c["id"] for c in cards}
        featured = newcomers[-1] if newcomers else (
            prior_featured[1] if prior_featured and prior_featured[1] in active else cards[-1]["id"])
        entries, portrait_changes = portraits(archive, path, sheet, cards)
        statistics = {address: value for address, value in chain_statistics(cells).items() if cells.get(address) != value}
        if statistics:
            entries[path] = update_sheet_cells(archive.read(path), statistics)
        catalog = render_catalog(cards, featured)
        stale_catalog = old_catalog != catalog
        stale_kits = old_kits != {"cards": kits}
        portrait_payload = {
            "cards": [{"cardId": card["id"], "portrait": card["portrait"]} for card in cards]
        }
        old_portraits = json.loads(PORTRAITS.read_text(encoding="utf-8-sig")) if PORTRAITS.exists() else None
        stale_portraits = old_portraits != portrait_payload
        if not check and workbook.read_bytes() != origin:
            raise ValueError("同步期间工作簿已更新，请保存后重新打包")
        if check:
            if stale_catalog or stale_kits or stale_portraits or entries:
                raise ValueError("角色表、运行配置或立绘尚未同步")
        elif entries:
            result = io.BytesIO()
            with ZipFile(result, "w", ZIP_DEFLATED) as out:
                for item in archive.infolist():
                    out.writestr(item, entries.pop(item.filename, None) or archive.read(item))
                for name, data in entries.items():
                    out.writestr(name, data)
            payload = result.getvalue()
            with ZipFile(io.BytesIO(payload)) as verify:
                if verify.testzip() or load_sheet(verify)[2] != {**cells, **statistics} or portraits(verify, path, sheet, cards)[1]:
                    raise ValueError("立绘写回后工作簿校验失败")
            if workbook.read_bytes() != origin:
                raise ValueError("同步期间工作簿已更新，请保存后重新打包")
            backup = ROOT / ".validation" / f"character-before-sync-{digest(origin)[:16]}.xlsx"
            backup.parent.mkdir(exist_ok=True)
            if not backup.exists():
                atomic_write(backup, origin)
            atomic_write(workbook, payload)
        if not check:
            if stale_catalog:
                atomic_write(CATALOG, catalog.encode("utf-8"))
            if stale_kits:
                atomic_write(KITS, (json.dumps({"cards": kits}, ensure_ascii=False, indent=1)+"\n").encode("utf-8"))
            if stale_portraits:
                atomic_write(PORTRAITS, (json.dumps(portrait_payload, ensure_ascii=False, indent=1)+"\n").encode("utf-8"))
    print(f"CHARACTER_SHEET_PASS online={len(cards)} featured={featured} portraits_updated={portrait_changes} chain_cells_updated={len(statistics)}", flush=True)


if __name__ == "__main__":
    args = argparse.ArgumentParser()
    args.add_argument("--check", action="store_true")
    args.add_argument("--workbook", type=Path, default=WORKBOOK)
    options = args.parse_args()
    try:
        sheet_sync(options.workbook, options.check)
    except (KeyError, OSError, ValueError) as error:
        raise SystemExit(f"角色表同步失败：{error}")

