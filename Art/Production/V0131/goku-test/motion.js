"use strict";

const byId = id => document.getElementById(id);
const stage = byId("stage");
const ctx = stage.getContext("2d");
const floor = 333;
const scale = .60;
const origin = 282;
const images = {};
const player = { action: 0, time: 0, frame: -1, start: null, handle: 0 };
const sweep = byId("sweep");
const controls = byId("frames");
const clamp = value => Math.max(0, Math.min(1, value));
const smooth = value => { const n = clamp(value); return n * n * (3 - 2 * n); };
const mix = (from, to, amount) => from + (to - from) * amount;

function clip() {
  return timeline(actions[player.action], sweep.checked);
}

function rootAt(track, time) {
  if (!track) return 0;
  const index = track.findLastIndex(point => point[0] <= time);
  if (index < 0) return track[0][1];
  const [start, from] = track[index];
  const next = track[index + 1];
  return next ? mix(from, next[1], smooth((time - start) / (next[0] - start))) : from;
}

function drawPose(context, id, x, baseline, factor) {
  const pose = drawings[id];
  const [left, top, width, height] = pose.crop;
  factor *= pose.zoom ?? 1;
  context.drawImage(images[pose.image], left, top, width, height,
    x + (left - pose.pivot[0]) * factor, baseline + (top - pose.pivot[1]) * factor,
    width * factor, height * factor);
}

function anchor(id, x, role, baseline = floor) {
  const pose = drawings[id];
  const point = pose[role];
  if (!point) return [x + 68, baseline - 165];
  const factor = scale * (pose.zoom ?? 1);
  return [x + (point[0] - pose.pivot[0]) * factor, baseline + (point[1] - pose.pivot[1]) * factor];
}

function ellipse(x, y, radiusX, radiusY, fill) {
  if (radiusX <= 0 || radiusY <= 0) return;
  ctx.beginPath();
  ctx.ellipse(x, y, radiusX, radiusY, 0, 0, Math.PI * 2);
  ctx.fillStyle = fill;
  ctx.fill();
}

function line(x, y, toX, toY, color, width) {
  ctx.beginPath();
  ctx.moveTo(x, y);
  ctx.lineTo(toX, toY);
  ctx.strokeStyle = color;
  ctx.lineWidth = width;
  ctx.stroke();
}

function arc(x, y, rx, ry, tilt, start, end, color, width) {
  ctx.beginPath();
  ctx.ellipse(x, y, rx, ry, tilt, start, end);
  ctx.strokeStyle = color;
  ctx.lineWidth = width;
  ctx.stroke();
}

function glow(x, y, radius, inner, outer) {
  if (radius < .5) return;
  const color = ctx.createRadialGradient(x, y, 0, x, y, radius);
  color.addColorStop(0, inner);
  color.addColorStop(1, outer);
  ellipse(x, y, radius, radius, color);
}

function drawHit(event, age, point) {
  if (age < 0 || age > 355) return;
  const [x, y] = point;
  const body = event === "body";
  const lift = event === "lift";
  const sweepHit = event === "sweep";
  const direction = lift ? -.65 : sweepHit ? .13 : 0;
  const exposure = 1 - smooth(age / 350);
  const burst = 9 + (body ? 21 : lift ? 17 : 12) * smooth(age / 59) + age * .094;
  ctx.save();
  ctx.globalAlpha = exposure;
  if (sweepHit) {
    arc(x - 41 + age * .10, y + 8, 69 + age * .06, 12 + age * .03, -.16,
      .07, Math.PI * .88, "#b37f43", 6);
    arc(x - 41 + age * .10, y + 5, 69 + age * .06, 11 + age * .03, -.16,
      .12, Math.PI * .75, "#fff1ba", 2.4);
  }
  if (lift) {
    arc(x - 28, y + 27, 12 + age * .06, 51 + age * .1, .80, Math.PI, Math.PI * 1.93, "#d99747", 4);
  }
  const angleShift = direction + (body ? .27 : .11);
  for (let n = 0; n < (body ? 12 : 8); n++) {
    const angle = angleShift + n * Math.PI / 4.7;
    const distance = burst * (.75 + (n % 5) * .19);
    const edgeX = x + Math.cos(angle) * distance;
    const edgeY = y + Math.sin(angle) * distance * (sweepHit ? .47 : .90);
    line(x + (edgeX - x) * .38, y + (edgeY - y) * .38, edgeX, edgeY,
      n % 3 ? "#c8873b" : "#fff2c1", n % 3 ? 1.7 : 3.6);
  }
  glow(x, y, 23 + age * .055, "#fff2b4", "#ffcc7900");
  ellipse(x, y, Math.max(0, (body ? 12 : 9) - age * .055),
    Math.max(0, 8 - age * .038), "#fffdf0");
  ctx.restore();
}

function hitMotion(entry, t, root) {
  const dt = entry[0] - t;
  if (dt < 0 || dt > 74) return;
  const strength = Math.sin((1 - dt / 74) * Math.PI / 2);
  const [x, y] = anchor(entry[2], origin + rootAt(root, entry[0]), "hit");
  ctx.save();
  ctx.globalAlpha = strength * .8;
  const lift = entry[1] === "lift";
  for (let n = 0; n < 5; n++) {
    const offset = (n - 2) * 4.5;
    const length = 17 + n * 5 + strength * 21;
    if (lift) line(x - length * .82 + offset, y + length, x - 4 + offset, y + 12, "#a8763a", 1.2);
    else line(x - length, y + offset, x - 10, y + offset + 1, "#a8763a", 1.2);
  }
  ctx.restore();
}

function glowOrb(x, y, time, beam, alpha) {
  const grow = smooth((time - beam.gather) / (beam.compress - beam.gather));
  const squeeze = smooth((time - beam.compress) / (beam.push - beam.compress));
  const launch = smooth((time - beam.push) / (beam.fire - beam.push));
  const radius = 5 + 22 * grow - 9 * squeeze + 17 * launch + Math.sin(time * .042) * 2 * grow;
  if (alpha <= 0) return;
  ctx.save();
  ctx.globalAlpha = alpha;
  glow(x, y, radius * 2.6, "#64eaffd8", "#3cbbff00");
  ellipse(x, y, radius, radius * .91, "#9af4ff");
  ellipse(x, y, radius * .64, radius * .59, "#feffff");
  for (let n = 0; n < 3; n++) {
    const tilt = time * .0024 + n * Math.PI / 3;
    arc(x, y, radius * (1.35 + n * .17), radius * (.63 + n * .18),
      tilt, -.9, 1.32, n % 2 ? "#70dbfa" : "#e8fcff", 1.2);
  }
  for (let n = 0; n < 12; n++) {
    const cycle = 1 - ((time - beam.gather + n * 47) % 480) / 480;
    const reach = radius + (20 + grow * 27) * cycle;
    const angle = n * Math.PI / 6 + time * .003;
    const startX = x + Math.cos(angle) * reach;
    const startY = y + Math.sin(angle) * reach;
    const extent = 3 + grow * 11;
    line(startX, startY, startX + Math.cos(angle) * extent, startY + Math.sin(angle) * extent, "#b4f9ff", 1.4);
  }
  ctx.restore();
}

function drawBeam(beam, point, t) {
  const [x, y] = point;
  if (t < beam.gather || t >= beam.extinguish) return;
  if (t < beam.fire) {
    glowOrb(x, y, t, beam, smooth((t - beam.gather) / 135));
    return;
  }

  const travel = smooth((t - beam.fire) / 162);
  const recoil = smooth((t - beam.fire) / 107);
  const tail = 1 - smooth((t - beam.cut) / (beam.extinguish - beam.cut));
  const length = Math.round((stage.width - x + 57) * travel);
  const width = (42 + 7 * Math.sin(t * .039) + 4 * Math.sin(t * .077)) * tail * mix(.60, 1, recoil);
  ctx.save();
  ctx.globalAlpha = tail;
  glow(x, y, 55 + 28 * tail, "#76e7ffd9", "#29c9ff00");
  if (length > 0) {
    const sections = 26;
    const edge = (n, sign, ratio) => {
      const u = n / sections;
      const ripple = 3.8 * Math.sin(u * 42 - t * .069) + 2.3 * Math.sin(u * 81 - t * .038);
      return [x + u * length, y + sign * ratio * (width * (.31 + .93 * u) + ripple) + u * Math.sin(t * .03) * 4];
    };
    for (const [ratio, color] of [[1.42, "#8ceeff55"], [1, "#167bc3"], [.78, "#1ed2ff"], [.52, "#bffaff"], [.31, "#ffffff"]]) {
      ctx.beginPath();
      ctx.moveTo(...edge(0, -1, ratio));
      for (let n = 1; n <= sections; n++) ctx.lineTo(...edge(n, -1, ratio));
      for (let n = sections; n >= 0; n--) ctx.lineTo(...edge(n, 1, ratio));
      ctx.closePath();
      ctx.fillStyle = color;
      ctx.fill();
    }
    for (let n = 0; n < 18; n++) {
      const u = ((t * (n % 3 + 4) + n * 191) % 1777) / 1777;
      const xx = x + u * length;
      const yy = y + ((n % 7) - 3) * (4 + u * width * .19);
      const end = Math.min(x + length, xx + 20 + (n % 5) * 17);
      line(xx, yy, end, yy + ((n % 3) - 1) * 3, n % 3 ? "#d5fbff" : "#297bcc", 1 + n % 3);
    }
  }
  ellipse(x, y, 15 + width * .15, 18 + width * .14, "#ffffff");
  const shock = 1 - smooth((t - beam.fire) / 225);
  if (shock > 0) {
    ctx.globalAlpha = Math.min(shock, tail);
    const expand = 18 + (t - beam.fire) * .53;
    arc(x + (t - beam.fire) * .28, y, expand * .43, expand, -.23, 0, Math.PI * 2, "#baf7ff", 3);
    for (let n = 0; n < 10; n++) {
      const angle = n * Math.PI / 5;
      line(x + Math.cos(angle) * 16, y + Math.sin(angle) * 16,
        x + Math.cos(angle) * expand * (1 + n % 3 * .09),
        y + Math.sin(angle) * expand, "#eefeff", n % 2 + 1.4);
    }
  }
  ctx.restore();
}

function passiveHalo(x, t, moment) {
  if (!moment || t < moment[0] || t > moment[1]) return;
  const age = t - moment[0];
  const alpha = smooth(age / 240) * smooth((moment[1] - t) / 340);
  ctx.save();
  ctx.globalAlpha = alpha * .62;
  arc(x, floor + 1, 69 + age * .038, 8 + age * .009, 0, 0, Math.PI * 2, "#c29847", 1.8);
  for (let n = 0; n < 7; n++) {
    const rise = ((age + n * 90) % 710) / 710;
    const xx = x + ((n * 27) % 142) - 71 + Math.sin(rise * 5 + n) * 6;
    ellipse(xx, floor - rise * 91, 1.3 + n % 3 * .37, 1.3 + n % 3 * .37, "#d7af63");
  }
  ctx.restore();
}

function beamAnchor(poses, index, time, x) {
  const here = poses[index];
  const from = anchor(here[1], x, "beam");
  const next = poses[index + 1];
  if (!next || !drawings[next[1]].beam) return from;
  const to = anchor(next[1], x, "beam");
  const distance = Math.hypot(to[0] - from[0], to[1] - from[1]);
  const blend = distance > 22 ? Math.min(32, next[0] - here[0]) : next[0] - here[0];
  const travel = smooth((time - next[0] + blend) / blend);
  return [mix(from[0], to[0], travel), mix(from[1], to[1], travel)];
}

function backdrop(beam, time) {
  const background = ctx.createLinearGradient(0, 0, 0, stage.height);
  background.addColorStop(0, "#faf7ef");
  background.addColorStop(1, "#e3e6da");
  ctx.fillStyle = background;
  ctx.fillRect(0, 0, stage.width, stage.height);
  if (beam) {
    const energy = smooth((time - beam.gather) / 950) * smooth((beam.extinguish - time) / 600);
    ctx.fillStyle = "rgba(30, 64, 75," + energy * .22 + ")";
    ctx.fillRect(0, 0, stage.width, stage.height);
  }
  for (let offset = 0; offset < 3; offset++) {
    const y = floor + 4 + 13 * offset;
    line(44, y, 916, y, "#9fae9d3a", 1);
  }
}

function paint(action, index, t) {
  const id = action.poses[index][1];
  const x = origin + rootAt(action.root, t);
  backdrop(action.beam, t);
  ellipse(x, floor + 5, 91, 10, "#21484326");
  passiveHalo(x, t, action.passive);
  for (const event of action.impacts ?? []) hitMotion(event, t, action.root);
  const breathing = action.loop ? 1 + .005 * Math.sin(t * Math.PI * 2 / action.duration) : 1;
  drawPose(ctx, id, x, floor, scale * breathing);
  for (const [time, family, drawing] of action.impacts ?? []) {
    drawHit(family, t - time, anchor(drawing, origin + rootAt(action.root, time), "hit"));
  }
  if (action.beam) drawBeam(action.beam, beamAnchor(action.poses, index, t, x), t);
}

function display(time) {
  const action = clip();
  player.time = Math.max(0, Math.min(time, action.duration));
  const index = action.poses.findLastIndex(([at]) => at <= player.time);
  paint(action, index, player.time);
  byId("scrub").value = player.time;
  byId("time").textContent = (player.time / 1000).toFixed(2) + " / " + (action.duration / 1000).toFixed(2) + " 秒";
  if (index !== player.frame) {
    player.frame = index;
    const [, , label] = action.poses.slice(0, index + 1).findLast(pose => pose[2]);
    byId("pose").textContent = label;
    const chapters = [...controls.children];
    for (const button of chapters) {
      if (button.dataset.label === label) button.setAttribute("aria-current", "true");
      else button.removeAttribute("aria-current");
    }
  }
}

function pause() {
  cancelAnimationFrame(player.handle);
  player.start = null;
  byId("play").textContent = "播放";
}

function play() {
  if (player.start !== null) return pause();
  if (player.time >= clip().duration) display(0);
  byId("play").textContent = "暂停";
  player.start = performance.now();
  function update(now) {
    const action = clip();
    const dt = Math.min(now - player.start, 50) * (byId("slow").checked ? .5 : 1);
    const target = player.time + dt;
    player.start = now;
    if (target >= action.duration && !action.loop) {
      display(action.duration);
      pause();
      return;
    }
    display(target % action.duration);
    player.handle = requestAnimationFrame(update);
  }
  player.handle = requestAnimationFrame(update);
}

function setup() {
  pause();
  player.frame = -1;
  const action = clip();
  byId("sweep-option").hidden = !action.branch;
  byId("note").textContent = action.note;
  byId("scrub").max = action.duration;
  const tabs = [...byId("tabs").children];
  tabs.forEach((tab, index) => tab.setAttribute("aria-pressed", String(index === player.action)));
  controls.replaceChildren(...action.poses.filter(([, , name]) => name).map(([time, id, label]) => {
    const button = document.createElement("button");
    const image = document.createElement("canvas");
    image.width = 122;
    image.height = 73;
    image.setAttribute("aria-hidden", "true");
    drawPose(image.getContext("2d"), id, 56, 68, .138);
    button.className = "frame";
    button.type = "button";
    button.dataset.label = label;
    button.append(image, label);
    button.addEventListener("click", () => { pause(); display(time); });
    return button;
  }));
  display(0);
}

function step(forward) {
  pause();
  const { poses, duration } = clip();
  const key = forward ? poses.find(([time]) => time > player.time) : poses.findLast(([time]) => time < player.time);
  display(key?.[0] ?? (forward ? duration : 0));
}

function buildTab(action, index) {
  const tab = document.createElement("button");
  tab.type = "button";
  tab.className = "tab";
  if (action.icon !== undefined) {
    const icon = document.createElement("span");
    icon.className = "icon";
    icon.setAttribute("aria-hidden", "true");
    if (action.icon === 3) icon.classList.add("passive-icon");
    else icon.style.backgroundPosition =
      (action.icon % 2 * 100) + "% " + (Math.floor(action.icon / 2) * 100) + "%";
    tab.append(icon);
  }
  tab.append(action.label);
  tab.addEventListener("click", () => { player.action = index; setup(); });
  return tab;
}

Promise.all([...new Set(Object.values(drawings).map(pose => pose.image))].map(async filename => {
  const image = new Image();
  image.src = filename;
  await image.decode();
  images[filename] = image;
})).then(() => {
  byId("tabs").replaceChildren(...actions.map(buildTab));
  byId("play").addEventListener("click", play);
  byId("previous").addEventListener("click", () => step(false));
  byId("next").addEventListener("click", () => step(true));
  byId("scrub").addEventListener("input", event => { pause(); display(Number(event.target.value)); });
  sweep.addEventListener("change", setup);
  setup();
}).catch(error => {
  console.error(error);
  byId("error").textContent = "动作图未能加载。请打开保留完整目录的预览。";
  byId("error").hidden = false;
});
