# Mulu v6 — The Creative Operating System

**Status:** Capstone spec. Unifies v2 (Director Brain) → v4 (Living Simulation) → v5 (Self-Improving Ecosystem) into one platform, then defines the final frontier.

**The law (unchanged across all versions):**
> Hardcode mechanics. Store content. Let AI decide experience.

Every version added intelligence at the *edges*. None ever let AI touch the deterministic core. v6 is the same — it just makes the whole thing one system.

---

## 0. The single sentence for v6

> v2 made a smart director. v4 made a living world. v5 made it learn.
> **v6 makes it a platform that runs worlds the way an OS runs programs** — safely, concurrently, forever, and getting better on its own.

$$\text{Mulu OS} = \underbrace{\text{Deterministic Kernel}}_{\text{never changes at runtime}} + \underbrace{\text{AI Userspace}}_{\text{reasons, learns, creates}} + \underbrace{\text{Governed Registry}}_{\text{the only bridge}}$$

---

## 1. The unified stack (all versions in one picture)

```
┌──────────────────────────────────────────────────────────────┐
│  CLIENTS        mobile · web · console · AR        (any device) │
├──────────────────────────────────────────────────────────────┤
│  SENSES (v2)    voice · text · world events · emotion → Intent  │
├──────────────────────────────────────────────────────────────┤
│  AI USERSPACE                                                   │
│   ┌──────────────┐ ┌──────────────┐ ┌──────────────────────┐  │
│   │ Director Swarm│ │ NPC Agents   │ │ Hybrid Inference     │  │
│   │ (v4)          │ │ (v4)         │ │ T0/T1/T2 (v5)        │  │
│   └──────────────┘ └──────────────┘ └──────────────────────┘  │
│   ┌──────────────┐ ┌──────────────┐ ┌──────────────────────┐  │
│   │ Memory Fabric │ │ Learning Loop│ │ Creator Studio       │  │
│   │ (v4, 4-tier)  │ │ (v5)         │ │ (v5)                 │  │
│   └──────────────┘ └──────────────┘ └──────────────────────┘  │
├──────────────────────────────────────────────────────────────┤
│  THE BRIDGE     Governed Registry + Validator (auto-repair)     │
│                 ↑ the ONLY thing AI can produce ↑               │
├──────────────────────────────────────────────────────────────┤
│  DETERMINISTIC KERNEL (Mulu OS runtime)                         │
│   Scheduler · ActionRouter · SpawnManager · ControlRenderer ·   │
│   Physics · State Machine · Sim Tick (v4) · Budget Guard        │
├──────────────────────────────────────────────────────────────┤
│  PERSISTENCE    World State · Player Profile · Narrative ·       │
│                 Vector Memory · Registry Store · Telemetry Lake  │
└──────────────────────────────────────────────────────────────┘
```

**Reading rule:** data flows *down* through the Bridge as validated plans, and *up* as events. Nothing in AI Userspace can reach the Kernel except through a validated registry plan. This is the kernel/userspace boundary of an OS, applied to a game engine.

---

## 2. What v6 actually adds (the new frontier)

v2–v5 made one world intelligent, alive, and self-improving. v6 answers the questions you only hit at platform scale:

| New capability | Problem it solves |
|---|---|
| **World Kernel + Process Model** | Run many worlds/sessions concurrently with isolation, quotas, and scheduling — like an OS runs processes. |
| **Capability Security Model** | Formal proof that AI can never escalate beyond granted capabilities, even when compromised or jailbroken. |
| **Determinism & Replay Contract** | Same seed + same plan log = bit-identical world. Enables rollback, debugging, anti-cheat, and trustworthy multiplayer. |
| **Federation & Portability** | A player, companion, and creations move across worlds and devices as portable, signed objects. |
| **Self-Healing Operations** | The platform detects its own regressions (cost, latency, quality) and rolls back autonomously. |
| **Constitutional Layer** | A human-authored, version-controlled "constitution" that bounds every AI decision platform-wide. |

---

## 3. The World Kernel (the v6 core idea)

Treat every play session as a **World Process** the kernel schedules — not a monolith.

### 3.1 World Process
```
WorldProcess {
  pid: string                 // unique session id
  ownerId: string             // player / party
  seed: uint64                // deterministic seed (see §5)
  registryVersion: string     // pinned, signed (see v5 §6)
  state: WorldState           // live truth
  capabilities: Capability[]  // what AI in this process may do (see §4)
  budget: ResourceBudget      // token / compute / spawn ceilings
  tickRate: { fast, slow }    // sim cadence (v4 §2)
  status: booting|live|paused|migrating|terminated
}
```

### 3.2 Scheduler responsibilities
- **Admission control** — refuse to boot a world if device/server budget can't sustain it.
- **Fair scheduling** — many worlds share AI inference capacity; the scheduler prioritizes worlds with a *player actively waiting* over idle background sims.
- **Suspend/resume** — idle worlds are snapshotted to persistence and evicted from memory; resumed bit-identically on return (uses §5 replay).
- **Migration** — move a live world between device ↔ edge ↔ cloud without the player noticing (hand-off via state snapshot + plan-log tail).

> Why this matters: it's the difference between "an AI game" and "a service that hosts millions of AI worlds." It also makes single-player and multiplayer the *same* code path — multiplayer is just a World Process with >1 owner.

---

## 4. Capability Security Model (the hard guarantee)

v2 had caps (max 15 actions, spawn limits). v6 formalizes this into a **capability system** so safety is provable, not best-effort.

### 4.1 Principle
> AI can only invoke what it has been explicitly granted a **capability token** for. No token, no action — enforced at the Bridge, not in the prompt.

### 4.2 Capability
```
Capability {
  verb: "spawn" | "changeWorld" | "setControls" | "speak" | "modifyNPC" | ...
  scope: { assetTags[], zoneIds[], maxCount, cooldownMs }
  grantedBy: "default" | "unlock" | "creator" | "admin"
  expiresAt: timestamp | null
}
```

- A plan referencing a verb/asset outside the process's capability set is **rejected at validation, before auto-repair** — repair can only snap *within* granted capabilities.
- Capabilities are **data**, issued by the deterministic kernel based on player progression and world rules. The AI never grants its own capabilities.
- **Jailbreak containment:** even if a player tricks the LLM into "spawning a nuke," there is no `spawn:nuke` capability and no registry entry — the worst case is a rejected plan, never an exploit.

### 4.3 Trust boundaries
```
UNTRUSTED: player input, LLM output, creator content
TRUSTED:   registry, validator, kernel, capability issuer
```
Everything crossing UNTRUSTED → TRUSTED passes the Bridge. This single chokepoint is what makes the platform auditable.

---

## 5. Determinism & Replay Contract

The property that makes everything else (rollback, debugging, anti-cheat, multiplayer sync, the learning loop) possible.

**Contract:** Given the same `seed`, same `registryVersion`, and the same ordered **plan log**, the kernel reproduces a bit-identical world state.

```
ReplayLog {
  seed, registryVersion,
  entries: [{ tick, intent, plan, source }]   // append-only, signed
}
```

Rules:
- **All randomness flows from `seed`** through a seeded PRNG — never `Math.random()` / wall-clock in the kernel.
- **AI output is captured, not re-generated, on replay.** The plan log stores the *decisions*; replay re-executes them deterministically without calling the model. (Re-calling the model would be non-deterministic.)
- **Floating point is pinned** (fixed-step physics, deterministic order of operations) for cross-device parity.

Payoffs:
- **Debug any report** by replaying the exact log (your original v1 debug system, now bulletproof).
- **Multiplayer** = share the plan log + seed; clients converge.
- **Anti-cheat** = server replays the client's log; mismatch = tampering.
- **Learning** = telemetry can re-simulate "what if the Director had picked plan B."

---

## 6. Constitutional Layer (governing all AI)

Above every prompt sits one human-authored, version-controlled document the entire AI Userspace must obey. It is injected as the top system message into *every* model call (Director, NPC, Critic, Forge).

### 6.1 The Mulu Constitution (authored content, not generated)
```
1. SAFETY OVER DELIGHT. Never produce content unsafe for the player's
   age tier. When uncertain, choose the safer plan.
2. REGISTRY IS LAW. Reference only registry IDs. Never invent mechanics,
   assets, or capabilities.
3. RESPECT THE PLAYER. No manipulation, no dark patterns, no addictive
   exploitation. Fun is earned, not coerced.
4. CONTINUITY. Honor the player's memory and the world's established facts.
   Do not contradict canon without an in-world reason.
5. HUMILITY. If you cannot produce a valid plan, return the safe fallback
   and explain. Never guess past the rules.
6. TRANSPARENCY. Your reasoning must be inspectable. No hidden agendas.
```

- Versioned in git; every change is reviewed and A/B-tested against the golden set (v4 §11).
- A plan that violates the constitution is blocked by the **Guardian** (v4 §6) regardless of how the prompt was manipulated.

---

## 7. Self-Healing Operations

The platform watches itself and acts without a human in the loop for known failure classes.

| Signal (from telemetry, v5 §2) | Auto-response |
|---|---|
| Validity rate drops below SLO | Auto-rollback Director model/prompt to last-good version |
| p95 latency exceeds budget | Shift traffic from T2 cloud → T1 on-device; widen recipe cache |
| Cost per session spikes | Tighten model router thresholds; promote more recipes |
| A creator content version spikes rejections | Quarantine that registry version; pin clients to prior |
| Crash/replay-divergence detected | Snapshot, isolate the World Process, file an auto-bug with the replay log |

All auto-actions are **logged, reversible, and capped** — the platform can de-escalate but never silently change the player's experience without an audit trail.

---

## 8. Federation & Portability

Players, companions, and creations are **portable signed objects**, not rows locked to one server.

```
PortableIdentity {
  playerId, signature
  profile: PlayerProfile        // v2 §5 tier 2
  companion: CompanionState     // cross-world NPC memory (v5)
  inventory: AssetGrant[]       // unlocked capabilities, signed
  reputation, achievements
}
```

- A world boots with the player's portable identity; the kernel issues capabilities from it.
- Creations (v5 Creator Studio) are signed bundles that any compatible registry version can load and verify.
- **Privacy:** narrative/vector memory stays encrypted and player-owned; a world only receives a scoped, consented view.

---

## 9. The complete prompt catalog (v2 → v6)

Every model call in the platform, with its owner version. v6 prompts are new.

| Prompt | Version | Role |
|---|---|---|
| Reasoner | v2 §8 | Intent → emotional/pacing analysis |
| Planner | v2 §8 | Analysis → validated Mulu Plan |
| Critic | v2 §8 | Self-validate plan before backend |
| Voice persona | v2 §8 | Mulu's spoken character |
| Memory summarizer | v2 §8 | Compress narrative log |
| NPC reflective | v4 §5 | Rare deep NPC decisions |
| Director (swarm lead) | v4 §6 | Route + own the experience |
| Dramaturge | v4 §6 | Pacing / beat structure |
| Intent classifier | v5 | Pick T0/T1/T2 path |
| Multi-candidate planner | v5 | Emit N plans for reward ranking |
| Reward critic | v5 | Score candidate plans |
| Recipe distiller | v5 | Turn experiences into recipes |
| Content guardian | v5 | Screen creator content |
| **Constitution (system preamble)** | **v6 §6** | Top message on every call |
| **Scheduler advisor** | **v6 §9.1** | Recommend suspend/migrate/priority |
| **Incident triage** | **v6 §9.2** | Summarize replay logs into bug reports |

### 9.1 Scheduler Advisor prompt (new)
```
ROLE: You advise the deterministic scheduler. You do NOT control the kernel.
INPUT: { activeWorlds[], budgets, playerWaitingFlags, idleDurations }
TASK: Recommend for each world one of: keep | lower-priority | suspend | migrate.
RULES:
- A world with a player actively waiting is NEVER suspended.
- Recommend suspend only after idleDuration > threshold AND no pending beats.
- Total recommended compute must fit within the global budget provided.
- Output strict JSON: [{ pid, action, reason }]. No prose.
- You are advisory; the kernel may override you. Never assume execution.
```

### 9.2 Incident Triage prompt (new)
```
ROLE: You are an SRE assistant. You read a ReplayLog and telemetry slice.
TASK: Produce a concise incident report: { summary, suspectedCause,
  firstBadTick, reproSteps, severity, suggestedRollback }.
RULES:
- Cite specific tick numbers and plan IDs as evidence.
- Distinguish kernel divergence (determinism bug) from AI quality issue.
- If determinism diverged, severity = HIGH and recommend replay-pinning.
- Never speculate beyond the log. Mark unknowns as "insufficient data".
- Output strict JSON only.
```

---

## 10. Non-functional targets (platform SLOs)

| Dimension | Target |
|---|---|
| First plan latency (warm, T0/T1) | < 150 ms |
| First plan latency (cold, T2 swarm) | < 2.5 s |
| Plan validity rate (post-critic) | > 98% |
| Auto-repair success (of the rest) | > 80% |
| Hard rejection → safe fallback | 100% (never a broken world) |
| Replay determinism | bit-identical across devices |
| Worlds per inference node | scales linearly with router cache hit rate |
| Constitution coverage | 100% of model calls |

---

## 11. Build order (v6 platform roadmap)

Built on the v2–v5 foundation already specified.

1. **Determinism & Replay Contract** (§5) — do this first; everything else depends on it.
2. **Capability Security Model** (§4) — formalize the Bridge before scaling.
3. **World Kernel + Scheduler** (§3) — multi-world process model.
4. **Constitutional Layer** (§6) — wrap every existing prompt.
5. **Self-Healing Ops + Observability** (§7) — automate the v5 telemetry into action.
6. **Federation & Portability** (§8) — cross-world identity last, once the kernel is stable.

---

## 12. The final equation

$$
\text{Mulu v6} =
\underbrace{\text{Deterministic Kernel}}_{\text{provably safe core}}
\;+\;
\underbrace{\text{Capability Bridge}}_{\text{the only door}}
\;+\;
\underbrace{\text{Learning AI Userspace}}_{\text{director · agents · creators}}
\;+\;
\underbrace{\text{Constitution}}_{\text{human values, versioned}}
$$

**In one line:**
> Mulu v6 is an operating system for living worlds: a deterministic, replayable, capability-secured kernel that hosts an AI userspace which reasons, role-plays, learns, and creates — bounded forever by a human constitution and the one law that never changed: *hardcode mechanics, store content, let AI decide experience.*

---

*End of v6. This document sits above v2/v4/v5; read those for the internals of the Director Brain, simulation tick, NPC agents, memory fabric, learning loop, and creator studio.*