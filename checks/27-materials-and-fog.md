# checks/27-materials-and-fog

**Unit 27.** Occlusion, fog, materials, reflection. No *ossia score* required.

## Shaders

| id | source | notes |
|:---|:-------|:------|
| `27-materials` | `library/shaders/27/materials.fs` | AO, height fog, material ids, one bounce of reflection with Fresnel, step view |

## Attribution

The five-sample occlusion estimator is Inigo Quilez's.

## A bug the guard caught before CI did

The reflection amount was first named `reflect`, which is a GLSL built-in
function. `scripts/isf.py` refused it at parse time with a message naming the
input, rather than letting it reach glslang in CI as it did three times before
the guard existed. It is now `mirror`. **This is the guard working as intended**
and it is worth recording that it paid for itself.

## Design notes

Occlusion is applied to ambient light only, not to the whole result. The unit
says why: the key light already has a shadow, and multiplying everything by AO
makes crevices black rather than darker. That is the most common way this
technique is misused.

**Knowing when to stop** is a ranked list in the unit rather than a paragraph,
because the ordering is the actual content: items 1 to 4 cost almost nothing and
account for most of the difference between a render and a picture, and items 6
onward are where budgets go.

## Re-verify when

- **The shader is edited.** The unit quotes the occlusion loop verbatim and its
  steps depend on the four views and on the Fog settles and Roughness controls.

## Corrections and open questions

- Also lights in code space; see `checks/26-lighting.md`.
- The refraction exercise is not implemented anywhere. If a reference solution
  is added it belongs in `library/shaders/27/`.
