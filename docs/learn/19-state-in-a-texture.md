---
layout: default
title: "Unit 19: Particles and simulation with state in a texture"
description: "A persistent buffer holding numbers rather than a picture is a simulation. Reaction-diffusion in forty lines, why a fragment shader cannot have true particles, and what to use instead."
parent: Units
nav_order: 21
unit: "19"
permalink: /learn/19-state-in-a-texture.html
reading_time: "14 min"
practice_time: "35 min"
glsl: "GLSL ES 3.00"
---

# Unit 19: Particles and simulation with state in a texture

{% include unit_meta.html %}

> **Before this unit** read [Unit 18]({{ site.baseurl }}/learn/18-feedback.html).
>
> **You will need** the player below, and the patience to let it run for thirty seconds.
>
> **You will build** a Gray-Scott reaction-diffusion simulation, and an accurate idea of what a fragment shader can and cannot simulate.

## Why this matters

[Unit 18]({{ site.baseurl }}/learn/18-feedback.html)'s buffer held a picture. Nothing requires that. A buffer holds four numbers per texel, and if those numbers are the state of something rather than a colour, the shader becomes a simulation running entirely on the GPU with no data leaving it.

That is how fluid, smoke, cloth, flocking, cellular automata, and reaction-diffusion are done in real-time graphics, and it is the technique with the highest ratio of visual result to code in this whole course. The simulation below is forty lines and it produces coral, mitosis, worms, and spots depending on two numbers.

It also has a hard limit, and this unit is honest about it early, because a lot of tutorial material is not. A fragment shader can only **gather**: it computes the value at its own position by reading other positions. It cannot **scatter**: it cannot decide to write somewhere else. That makes field simulations natural and true particle systems impossible.

## The idea

**Store state, not colour.** A `vec4` per texel is four numbers. In this unit's shader, two of them are the concentrations of two chemicals; the other two are unused. In a fluid solver they would be a velocity; in a particle field, a position and a lifetime.

**Use a float buffer, always.** A simulation stepped in 8-bit precision quantises its own state every frame, and the error accumulates in a way that looks like an instability in the model rather than in the storage.

**Gather is the only access pattern.** Each invocation writes exactly one texel: its own. It may read anywhere. This makes anything expressible as "the new value here depends on the old values near here" straightforward, which covers diffusion, advection, convolution, and every cellular automaton.

**The Laplacian is how a shader sees its neighbours.**

```glsl
vec2 lap = 0.20 * (left + right + up + down)
         + 0.05 * (four diagonals)
         - centre;
```

This is the discrete second derivative, and it is the diffusion term in almost every model you will meet. Note that the neighbours come from a **texture**, not from the screen: a fragment shader cannot see what another fragment computed this frame, but it can read what every fragment computed last frame.

**Gray-Scott, in two lines.** Two chemicals, A and B. B converts A into more of itself, so it is autocatalytic; A is fed in from outside at rate `feed`; B is removed at rate `kill + feed`. Both diffuse, at different speeds.

```glsl
A += dt * (Da * lap(A) - A*B*B + feed * (1.0 - A));
B += dt * (Db * lap(B) + A*B*B - (kill + feed) * B);
```

Every pattern in the model comes from the competition between those. `feed` and `kill` are the only two numbers that matter, and a map of the patterns they produce is one of the more beautiful pictures in mathematics.

**A step is a pass.** The Laplacian reads the texture, so a loop inside one pass would apply the reaction several times against a neighbourhood that never updated. That diverges into a checkerboard within a second. Several steps means several passes, each naming the same persistent target.

**A seed is not decoration.** The model does nothing at all from a uniform state; it is a stable equilibrium. Every pattern you will ever see grew from a disturbance.

### Why there are no particles here

A particle system needs each particle to write itself into the frame at wherever it has moved to. That is a scatter, and a fragment shader cannot do it. Three ways round it:

**A vertex shader can scatter**, because a vertex's whole job is to decide where it lands. Give it one vertex per particle, have it read that particle's state from a texture, and place it. This is how particle systems are actually built, and it is [Unit 31]({{ site.baseurl }}/learn/31-vertex-shaders.html).

**A compute shader can write anywhere**, with atomics to resolve collisions. This is the modern answer and it is [Unit 32]({{ site.baseurl }}/learn/32-compute-shaders.html).

**Or do not scatter.** Simulate a *field* rather than a set of points: advect a density, diffuse a chemical, evolve an automaton. That is what this unit does, and for a great deal of visual work it is the better answer anyway, because a field has no particle count to run out of.

## Build it

{% include shader.html id="19-reaction" height="460" pointer="seedAt" caption="Give it thirty seconds. Turn Seed while dragging on and drag the canvas to inject more of chemical B, which is how you draw into a simulation that has no drawing operation at all." %}

1. **Watch it from the beginning.** A single blob of B in a field of A. It grows, becomes unstable at its edge, branches, and fills the space. Nothing is scripted; the whole picture is two numbers.
2. **Step through the recipes.** Coral branches. Mitosis divides into cells that split. Worms wander. Spots are stable and static. Waves pulse. Every one of these is the same forty lines with a different `feed` and `kill`.
3. **Switch to custom and move Feed and Kill slowly.** Small moves, on the order of 0.001. The parameter space is mostly empty and the interesting regions are narrow, which is why the recipes exist.
4. **Press Clear and watch the initial condition.** A disc of B and nothing else.
5. **Turn Seed while dragging on and drag.** You are adding chemical, not pixels. The pattern grows from where you put it, according to its own rules.
6. **Take Step size above about 1.2.** The simulation goes unstable and fills with a checkerboard, which is what an explicit integrator does when the step is too large for the diffusion. Worth causing on purpose once.
7. **Change Diffuse B.** Turing's original insight was that a pattern arises when two things diffuse at *different* rates. Set both diffusion rates equal and watch the pattern refuse to form.
8. **Look at the PASSES block in the source.** Four passes name `state`, then one draws. Those four are four real simulation steps, because a persistent target swaps after the pass that wrote it rather than at the end of the frame.

## Look at these

{% include toy.html id="XlsczN" title="Reaction diffusion" by="Shadertoy community" note="The same model with a parameter map, so you can see where in feed-kill space each pattern lives." %}
{% include toy.html id="4tGfDW" title="Fluid simulation" by="Shadertoy community" note="Semi-Lagrangian advection with pressure projection, all in gather form. The next step up from this unit." %}
{% include toy.html id="XlfGRj" title="Game of Life" by="Shadertoy community" note="The simplest possible state-in-a-texture shader; read it if the reaction-diffusion is too much at once." %}

Karl Sims's [reaction-diffusion tutorial](https://www.karlsims.com/rd.html) is the clearest explanation of the model anywhere.

## Common mistakes

- **Iterating inside one pass.** The Laplacian reads the texture, so the neighbourhood does not update and the iteration diverges. This unit's shader was written that way first and produced a checkerboard within a second; several steps means several passes.
- **An 8-bit state buffer.** The quantisation looks like a fault in the model.
- **No seed**, so nothing ever happens and the shader looks broken.
- **A step size the integrator cannot take.** Explicit Euler is stable only up to a limit set by the diffusion rates. Halve the step before blaming the model.
- **Simulating at full resolution.** A reaction-diffusion at half resolution looks the same and costs a quarter. This unit's shader runs its state at half, which is the `WIDTH` and `HEIGHT` keys in its `PASSES` block.
- **Expecting determinism across machines.** Float arithmetic differs slightly between drivers, and a chaotic system amplifies that. Two GPUs running this shader diverge visibly within a minute, which is worth knowing before a two-screen installation.

## Exercise

Build Conway's Game of Life as a two-pass ISF shader: one persistent pass holding the state, one display pass.

Requirements: the state buffer must be one cell per pixel of a target smaller than the output, so the cells are visible; the initial state comes from a hash, with a `float` named `density` controlling how much is alive; a `bool` named `step` freezes the simulation so a state can be examined; and a `point2D` plus a `bool` let a reader draw live cells.

**Success criterion:** a glider moves diagonally without changing shape, forever, and reaches the edge without artefacts. If it decays or grows, the neighbour count is wrong; if it distorts, the state buffer is being filtered rather than sampled at texel centres, which is the one thing in this exercise that will not be obvious.

## Going further

- [Karl Sims, reaction-diffusion](https://www.karlsims.com/rd.html), the clearest explanation of the model.
- [Jos Stam, *Real-Time Fluid Dynamics for Games*](https://www.dgp.toronto.edu/public_user/stam/reality/Research/pdf/GDC03.pdf), the paper behind most shader fluid solvers.
- [Alan Turing, *The Chemical Basis of Morphogenesis*](https://royalsocietypublishing.org/doi/10.1098/rstb.1952.0012), 1952, which is where the idea that two diffusion rates make a pattern comes from.
- [*ossia score*'s compute shaders]({{ site.docs_baseurl }}/processes/compute-shaders.html), for the version that can scatter.
