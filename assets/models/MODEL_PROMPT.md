# Model Prompt Template — MOBA Characters

This document is the reusable recipe for generating character `.glb` files
through any text-to-3D tool — [Meshy](https://meshy.ai), [Tripo3D](https://tripo3d.ai),
[Luma Genie](https://lumalabs.ai/genie), [Rodin](https://hyperhuman.deemos.com),
or similar. It pairs a parameterised text prompt with a worked knight example,
the workflow steps each tool needs, and the export settings that make the
output compatible with the MOBA engine's RH Y-up convention (ADR-001) and the
M2 glTF loader (ADR-013).

It is a *working document*: edit it as the tools evolve and as more characters
teach us new prompt tricks. It is not an ADR — no architectural decisions live
here.

**Tool variability (important).** Some tools (Meshy) expose **separate positive
and negative prompt fields**; others (Tripo3D Free) only accept **a single
combined text input**. The consolidated single-prompt form (section 4a) works
for both — paste it whole into the single field, or split the trailing
`No …` clause into the negative field if the tool has one.

---

## 1. Workflow

The shape of the workflow is the same across tools — generate, pick, refine,
(optionally) rig, (optionally) animate, export. Tool-specific differences
are flagged below.

1. **Text-to-3D → New Generation.**
   - Style preset: **Cartoon** (Meshy) / **Stylized** (Tripo) / equivalent.
   - Polycount: **5,000–10,000 triangles** if the tool exposes it. LoL
     base champions ship at 5–7k; this is the comfortable range for our
     top-down camera. Tools that don't expose polycount → handle in §6a.
   - Symmetry: **On** for humanoids.
   - Prompt: the single block from §2 / §4a. Tools with a split prompt
     field: positive part on top, the trailing `No …` sentence in the
     negative field.
2. **Pick a candidate.** Most tools generate 3–4 previews. Pick the one
   whose silhouette reads cleanest at small size — that's the
   Brawl-Stars-thumbnail test.
3. **Refine.** Tool-specific. Meshy: "Refine" button. Tripo: select the
   candidate and request a high-quality re-render. Goal: production-quality
   mesh + baked textures.
4. **Auto-Rig.** Tool-specific availability:
   - **Meshy** has built-in auto-rig (Character mode), Mixamo-compatible
     bone names.
   - **Tripo Free** does not auto-rig — upload the mesh to
     [Mixamo](https://www.mixamo.com) (free, Adobe account) for auto-rig.
   - Verify hierarchy in the preview before continuing — visibly displaced
     spine bones mean re-run or fix in Blender.
5. **Animate.** Pick from the tool's library (Meshy or Mixamo). Each clip
   becomes one animation in the exported file. See §5 for the canonical
   animation list per character.
6. **Decimate (only if the source exceeded the polycount target).** See §6a.
7. **Export.** Format **`.glb`**, embedded textures **on**, Y-up, scale 1.0.
   Save as `assets/models/<character-name>.glb`.

---

## 2. Prompt template

Copy this single block and fill the bracketed placeholders. Keep the
**bold-italic** parts unchanged — they anchor the visual family across
characters so every hero in the game looks like it belongs to the same world.

**Hard rule: never include a weapon in the character prompt.** Text-to-3D
cannot generate believable hand-grip geometry; the weapon ends up floating,
in the wrong hand, or fused to the wrist. Generate the character empty-handed
in a strict T-pose, then generate the weapon as a separate prop (section 4b),
and attach it at runtime to the character's hand bone in a later iteration.
This is also how Mixamo / Unreal / Unity all expect rigged characters:
weapons are bone-attached props, not part of the body mesh.

```text
Vitruvian Man T-pose: arms perfectly horizontal at shoulder height, palms
facing downward toward the floor, fingers held flat together, legs straight
and slightly apart, hands empty. A [archetype] [age/body] in [defining
outfit/armor with materials and colors][, with non-hand accessory if any].

***Stylized chunky chibi proportions, big head, simplified cartoon mitten
hands with four fingers and a thumb pressed flat together, closed neutral
mouth. Cell-shaded cartoon look inspired by Brawl Stars[ and <secondary
reference>]. Clean low-poly stylized topology, symmetric pose, single
base-color texture atlas.***

No weapon, no sword, no shield, no extra fingers, no polydactyly, no bent
arms, no A-pose, no twisted spine, no multiple materials, no separate
texture per body part.
```

Three things make this prompt robust across tools:

1. **"Vitruvian Man T-pose"** as the very first tokens — the iconic
   palms-down arms-horizontal pose, deeply anchored in the training set.
   Plain "T-pose" alone is too weak; AI models default to A-pose
   (palms-forward) without this reinforcement.
2. **Style anchor in the middle** (bold-italic) — keeps the visual family
   consistent across the cast.
3. **"No …" clause at the end** — single-prompt tools (Tripo Free) read it
   as soft negatives; split-prompt tools (Meshy) pipe it into the negative
   field. Same prompt, both tools.

Tools that accept a separate negative prompt: copy the trailing `No …`
sentence into their negative field for stronger suppression.

### Placeholder guidance

| Placeholder | What works well | What goes wrong |
|---|---|---|
| `[archetype]` | concrete fantasy noun: "knight", "rogue", "tundra shaman", "swamp witch". | abstract roles ("hero", "warrior") give bland output. |
| `[age/body]` | "young", "burly old", "lithe", "stocky". | very specific ages ("23 years old") confuse the model. |
| `[outfit / armor]` | name materials *and* colors: "polished silver plate with gold trim", "boiled leather with cyan glow runes". | one-word ("armor") gives generic; nine adjectives ("rusty intricate ornate dark gritty …") gives muddy. |
| `[non-hand accessory]` (optional) | things the body wears that don't interfere with the rig: "flowing crimson cape", "feathered shoulder pauldron", "tribal face paint". | hand-held items (weapons, shields, staves) — those are *always* a separate prop, never in the character prompt. |
| `<secondary reference>` (optional) | a single recognisable game/movie character: "League of Legends' Garen", "Studio Ghibli's Howl". | listing more than one reference confuses the style mix. |

The **bold-italic** style anchor is non-negotiable across the character cast.
If you want a character to look stylistically different (e.g. an undead unit),
write a new template, do not weaken this one.

---

## 3. Extended negative-prompt vocabulary

The template's trailing `No …` clause covers the must-have suppressions.
If your tool exposes a separate negative-prompt field and your character
keeps producing recurring failures, this richer vocabulary can go in:

```text
photorealistic, pbr, realistic skin pores, gritty textures, deformed limbs,
deformed hands, extra fingers, six fingers, polydactyly, splayed fingers,
mangled fingers, extra digits, weapon clipping body, complicated background,
multiple weapons, floating accessories, ornate clutter, A-pose, bent arms,
twisted spine, asymmetric pose, open mouth, separate texture per body part.
```

The medical term **polydactyly** (extra fingers) is unusually effective —
text-to-3D training data labels it as an anomaly to avoid, so it suppresses
six-finger hands much harder than plain "extra fingers" does.

---

## 4. Worked example — the knight (Garen-inspired)

Two separate generations: the empty-handed character, and the broadsword as
a standalone prop. They are merged later (bone attachment) — see section 4c.

### 4a. Character (empty-handed, strict T-pose)

Paste this single block verbatim into the text-to-3D field. Works for both
single-input tools (Tripo Free) and split-input tools (Meshy — paste the
trailing `No …` sentence into the negative field for extra strength).

> Vitruvian Man T-pose: arms perfectly horizontal at shoulder height, palms
> facing downward toward the floor, fingers held flat together, legs
> straight and slightly apart, hands empty. A heroic young fantasy knight in
> polished silver-and-gold plate armor with a flowing crimson cape.
> Stylized chunky chibi proportions, big head, simplified cartoon mitten
> hands with four fingers and a thumb pressed flat together, closed neutral
> mouth. Cell-shaded cartoon look inspired by Brawl Stars and League of
> Legends' Garen. Clean low-poly stylized topology, symmetric pose, single
> base-color texture atlas. No weapon, no sword, no shield, no extra
> fingers, no polydactyly, no bent arms, no A-pose, no twisted spine, no
> multiple materials, no separate texture per body part.

**Settings:** Style **Cartoon**, Art **Stylized**, **Polycount 5,000–10,000
triangles** (LoL-class for our top-down camera; see §6a), Symmetry **On**.

**Save as:** `assets/models/knight-garen.glb`.

### 4b. Sword prop (standalone)

Generate separately. No rigging, no animations — it's a static prop.

**Positive prompt:**

> A large two-handed fantasy broadsword lying flat in profile, blade pointing
> right, with an ornate silver crossguard with gold filigree accents, a
> leather-wrapped grip, and a round polished pommel. Stylized chunky cartoon
> weapon proportions matching a Brawl Stars hero weapon. Clean low-poly
> stylized topology, cell-shaded cartoon look.

**Negative prompt:**

> photorealistic, pbr, gritty textures, rust, blood, character, hand,
> complicated background, multiple weapons, sheath, scabbard.

**Settings:** Style **Cartoon**, Art **Stylized**, Polycount **Low**,
Symmetry **Off** (a sword is not bilaterally symmetric along the rendered
axis).

**Save as:** `assets/models/sword-broadsword.glb`.

### 4c. Attaching weapon to hand (later)

The character `.glb` carries the empty-handed body + skeleton. The sword
`.glb` carries the prop. The engine attaches the sword to the character's
right-hand bone (`mixamorig:RightHand` or equivalent) at runtime. That logic
is part of M2/M3, not part of this document. For now, both files just sit in
`assets/models/`.

---

## 5. Animations to apply

Pick these from Meshy's animation library after auto-rigging. Labels may
shift as Meshy updates — pick the closest equivalent and note any deviation
in section 7.

| Clip | Meshy label (as of 2026-05) | Notes |
|---|---|---|
| Idle | `Idle` (or `Stand Idle`) | Breathing loop. Required. |
| Walk | `Walking` | Forward, looped, in-place. Required. |
| Sword swing | `Sword Slash` or `1H Sword Attack` | Pick the clip with a horizontal arc. Two-handed swings often have no library equivalent yet — fall back to a 1H slash. |

The M1 engine state does no skeletal skinning yet, so these clips ride along
in the `.glb` unused until M3. They still need to be baked at generation
time — re-running Meshy later to add animations means re-generating the
character.

---

## 6. Export checklist

Match the project conventions to avoid manual fixes later:

- **Format:** `.glb` (single binary file, embedded textures).
- **Up axis:** **Y-up** — matches ADR-001 (right-handed Y-up).
- **Forward axis:** **−Z forward** — matches ADR-001. If the tool exports
  `+Z` forward, rotate 180° around Y in Blender before saving (or document
  the fix-up so the M2 loader can apply it).
- **Scale:** 1.0 (one tool unit = one world unit). Target world height for
  a humanoid ≈ 1.8. If the model exports much larger or smaller, scale in
  Blender before saving.
- **File name:** lowercase, hyphen-separated, descriptive. `knight-garen.glb`,
  `rogue-shadowstep.glb`, `mage-stormweaver.glb`.
- **Destination:** `assets/models/<file>.glb`.

### 6a. Decimation in Blender (when the source is over budget)

Some tools — notably Tripo3D — produce meshes far above our 5–10k-triangle
budget (their default is in the millions). A `.glb` over ~10 MB is a sign
you need to decimate.

1. **Open Blender.** `File → Import → glTF 2.0 (.glb / .gltf)` and select
   the source file.
2. **Select the character mesh** in the Outliner.
3. **Add a Decimate modifier.** Properties panel → wrench icon →
   `Add Modifier → Generate → Decimate`. Mode: **Collapse** (best for
   organic / soft shapes). Set **Ratio** so the resulting `Face Count`
   readout lands between **5,000 and 10,000 triangles**. For a Tripo source
   with ~2,000,000 triangles, ratio ≈ 0.005 lands around 10,000.
4. **Visually verify** — rotate the viewport, check the silhouette still
   reads (the cape, helmet, and shoulder pads keep their shape). If
   details collapse, raise the ratio.
5. **Apply the modifier.** Convert it to baked geometry: in the modifier
   panel's dropdown, `Apply`.
6. **Re-export.** `File → Export → glTF 2.0 (.glb)`. Settings:
   - Format: **glTF Binary (.glb)**.
   - Include: **Selected Objects** if you only want the character.
   - Transform: keep Y-up.
   - Geometry → Materials: **Export** (with images embedded).
   - Animation: leave on if rig + actions present.
7. **Replace** the source file in `assets/models/`.

Expected file-size impact from this step: factor 10–40 smaller. The Tripo
knight (~56 MB at 2 M triangles) lands around 3–6 MB at 10 k triangles
with the same textures embedded.

---

## 7. Troubleshooting log

Append observations as you discover tool quirks. Empty entries get pruned.

- *"Palms come out facing forward, not down."* Confirmed on Tripo3D
  (2026-05-29) — the dominant pose in text-to-3D training data is A-pose
  with palms-forward, so "palms down" alone is too weak. **Fix:** lead
  the prompt with **"Vitruvian Man T-pose"** (the iconic palms-down image,
  strongly anchored in training data) and explicitly say *"palms facing
  downward toward the floor"*. Put `A-pose, bent arms` in the negative
  vocabulary. This matters for rigging — Mixamo's auto-rigger expects
  palms-down bind pose; palms-forward causes wrong bone-roll on the upper
  arm and broken wrist twist when animations are applied.
- *"Weapon held wrong / fused to wrist / floating beside the hand."*
  Confirmed on first knight attempt (2026-05-29). Root cause: text-to-3D
  cannot generate believable hand-grip geometry. **Fix:** never put a
  weapon in the character prompt — generate the character empty-handed in
  T-pose (section 4a) and the weapon as a standalone prop (section 4b).
  Attach the prop to the hand bone at runtime (section 4c).
- *"Character not in T-pose — A-pose or random stance instead."*
  Confirmed on first knight attempt (2026-05-29). Root cause: pose hint was
  late in the prompt and weakly phrased ("relaxed T-pose with arms slightly
  raised" — Meshy reads this as an A-pose). **Fix:** put the pose sentence
  FIRST, use strict wording ("strict T-pose, arms perfectly horizontal at
  shoulder height, palms facing down"), and put `A-pose`, `bent arms`, and
  `arms down` into the negative prompt.
- *"Hands have six or more fingers / fingers splayed wildly."* Universal
  text-to-3D failure mode. **Fix:** in the positive prompt, force the hand
  style (*"simplified cartoon mitten hands with four fingers and a thumb
  held together"*) and the pose (*"palms facing down with fingers held flat
  together"*). In the negative prompt, add `polydactyly, six fingers,
  splayed fingers, extra digits`. The medical term **polydactyly** is the
  single most effective token here — it suppresses six-finger hands harder
  than plain "extra fingers".
- *"Cape clips through the body during walk."* Known Meshy auto-rig
  limitation for trailing cloth. Either accept (Brawl-Stars-style is
  forgiving) or remove the cape from the prompt.
- *"Auto-rig misplaced spine bones."* Re-run auto-rig, or accept the file
  and fix in Blender's Armature edit mode.
- *(add your own as you go)*

---

## 8. Reusing this template for a new character

1. Copy section 4 (the worked example block) to a new section below.
2. Swap the placeholders per the template in section 2.
3. **Keep the bold-italic style anchor identical** — that's what makes a
   visual family.
4. Update section 5 if any animations are character-specific (e.g. spell
   casts for a mage).
5. Generate, export, drop the `.glb` into `assets/models/`, link it from
   here under a "Character catalogue" subsection.

---

## 9. Character & prop catalogue

| Asset | Kind | Prompt section | File | Status |
|---|---|---|---|---|
| Knight (Garen-inspired) | character | §4a | `knight-garen.glb` | not yet generated |
| Broadsword | prop | §4b | `sword-broadsword.glb` | not yet generated |
