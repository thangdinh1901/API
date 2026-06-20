# Scene rotation & move (Plant 3D Composer)

## Modules

| File | Role |
|------|------|
| `Plant3DSkeletonManager/Core/TransformMath.cs` | C# scene graph: orbit, matrix, conjugation |
| `catalog_generator/p3d_composer/catalog_transforms.py` | Preview replay (undo bake → jogs → move → redo bake) |
| `catalog_generator/p3d_composer/scene_builder.py` | Build parts, delegates transforms to `catalog_transforms` |

## World axes

1. **Orbit** — `Origin` rotated about WCS `(0,0,0)` (C# only)
2. **Spin** — WCS rotate at build origin, then `move(origin)` in preview

## Object axes

- **No orbit** — `Origin` unchanged
- **Spin in place** — `move` to center, pivot-rotate, `move` back

## Catalog parts (flange, tee BW, reducer…)

`part.json` → `catalogFrameRotation` (usually `Ry 90°` from `CUST_*.py` bake).

Preview:

- `inv(R_cat)` on geometry before jogs
- WCS move: `t_build = inv(R_cat) × t_world`
- WCS/connection rotate: `inv(R_cat) × R_axis × R_cat`
- `R_cat` redo bake

Primitives and elbows (no bake) use identity frame — direct WCS move/rotate.

## Plant 3D constraint

`rotateX/Y/Z` pivot at **WCS (0,0,0)** — object jogs need pivot wrapper; world jogs rotate before final move.
