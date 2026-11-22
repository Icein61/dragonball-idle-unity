import { copyFile, mkdir, readFile, writeFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { runInNewContext } from "node:vm";
import path from "node:path";

const root = fileURLToPath(new URL("../", import.meta.url));
const source = path.join(root, "Art/Production/V0131/goku-test");
const target = path.join(root, "Assets/DragonIdle/Resources/Art/V0131/Goku");
const artwork = await readFile(path.join(source, "motion-art.js"), "utf8");
const timing = await readFile(path.join(source, "motion-timing.js"), "utf8");
const { drawings, moves } = runInNewContext(
  `${artwork}\n${timing}\n({ drawings, moves: [...actions, timeline(actions[2], true)] })`,
  {}, { timeout: 1500 }
);
const names = ["idle", "accent", "basic", "ultimate", "pursuit", "opening", "sweep"];
const hits = ["", "", "body", "", "lift", "", "sweep"];
const indices = new Map(Object.keys(drawings).map((name, index) => [name, index]));
const clips = moves.map((move, index) => ({
  id: names[index],
  title: move.label,
  duration: move.duration,
  impactAt: move.beam?.fire ?? move.impacts?.find(([, kind]) => kind === hits[index])?.[0] ?? 0,
  poses: move.poses.map(([at, drawing]) => {
    if (!indices.has(drawing)) throw new Error("Unknown pose " + drawing);
    return { at, drawing: indices.get(drawing) };
  }),
  root: (move.root ?? []).map(([at, x]) => ({ at, x })),
  impacts: (move.impacts ?? []).map(([at, kind, drawing]) => ({ at, kind, drawing: indices.get(drawing) })),
  ...(move.beam && { beam: move.beam }),
  ...(move.passive && { haloFrom: move.passive[0], haloTo: move.passive[1] })
}));
const atlas = Object.values(drawings).map(drawing => ({
  image: path.basename(drawing.image, ".png"),
  crop: drawing.crop,
  pivot: drawing.pivot,
  zoom: drawing.zoom ?? 1,
  hit: drawing.hit ?? [],
  beam: drawing.beam ?? []
}));

await mkdir(target, { recursive: true });
for (const name of new Set([...Object.values(drawings).map(drawing => drawing.image), "passive-training.png", "skill-icons.png"])) {
  await copyFile(path.join(source, name), path.join(target, name));
}
await writeFile(path.join(target, "Motion.json"), JSON.stringify({ atlas, clips }, null, 2) + "\n");
console.log(`Goku: ${atlas.length} drawings, ${clips.length} tracks (including sweep).`);
