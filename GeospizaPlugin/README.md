# Grasshopper Component Naming Conventions

## **1. Component Names**
- Use clear, descriptive names in **Pascal Case**.
    - Examples: `Construct Plane`, `Deconstruct Mesh`, `Offset Curve`.
- **Action Components:** Use verbs (e.g., `Move Geometry`, `Split Surface`).
- **Object Components:** Use nouns (e.g., `Point`, `Circle`, `Mesh Face`).

## **2. Inputs and Outputs**
- **Inputs:**
    - Use single letters or short names.
    - Examples: `G` (Geometry), `T` (Translation), `D` (Distance).
- **Outputs:**
    - Be descriptive and concise.
    - Examples: `O` (Origin), `N` (Normal), `R` (Result).

## **3. Categories and Subcategories**
- Organize logically under a **Category > Subcategory**.
    - Examples:
        - **Category**: `Transform`
        - **Subcategory**: `Affine`

## **4. Common Naming Patterns**
- **Construct Components**: Start with `Construct`.
    - Example: `ConstructPoint`
- **Deconstruct Components**: Start with `Deconstruct`.
    - Example: `DeconstructBrep`
- **Analysis Components**: Use verbs like `Analyze` or `Evaluate`.
    - Example: `EvaluateCurve`

## **5. Icons**
- Match Grasshopper’s flat, simple icon style.
- Use clear visuals that represent the functionality.

## **6. Consistency**
- Maintain consistent naming patterns across similar components.
    - Example: `TransformScale`, `TransformRotate`, `TransformMirror`.

## **7. Redundancy**
- Avoid redundant information in names.
    - Bad: `Mesh Tools - Mesh Weld`
    - Good: `MeshWeld`

## Example Component Breakdown
| **Component**       | **Inputs**                  | **Outputs**             | **Description**                                |
|----------------------|----------------------------|-------------------------|-----------------------------------------------|
| `UnfoldSheet`        | `S` (Sheet), `B` (Bends)   | `F` (Flat Sheet)        | Unfolds a sheet metal object.                 |
| `AnalyzeHoles`       | `S` (Sheet)                | `C` (Counts), `H` (Holes)| Counts and categorizes holes in a sheet.      |
| `OffsetEdge`         | `E` (Edge), `D` (Distance) | `O` (Offset Edge)       | Offsets the edge of a given surface or curve. |

