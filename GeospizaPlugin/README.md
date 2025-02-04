# Grasshopper Component Guidelines

## Naming Conventions

- Create only one component per .cs file
- Prefix Grasshopper component classes with `GH_` to avoid naming conflicts
- File names should start with `GH_` for easy identification

## Component Versioning

Based on discussions from the [Rhino forum](https://discourse.mcneel.com/t/plugins-new-version-best-approach/117964/4)
and [Parametric Camp](https://www.youtube.com/watch?v=uSIxFynt6ok&t=1081s).

### When to Version a Component

Versioning a component implies a minor version change (e.g., v0.1.0 to v0.2.0)

1. Obsolete (Deprecate) the old component if:

   - The behavior changes
   - Order of inputs/outputs changes
   - Default values change

2. Keep the same component if:
   - You are fixing bugs or improving performance
   - You change the category of the component
   - You change descriptions, icons, or names

### How to Deprecate Components

1. Create a copy of the component in the same .cs file
2. Add `_OBSOLETE` to the end of the class name (e.g., `Individual_OBSOLETE`)
3. Change the components exposure level to `Hiden`
4. Create new class with version number (e.g., `IndividualV2`)
5. Change the GUID of the new component

### Handling Obsolete Components

During minor releases:

1. Component Organization:

   - Keep all obsolete components in the codebase
   - Create an `Obsolete` folder if not already existing
   - Name deprecated files as `GH_ComponentName_OBSOLETE.cs`

2. File Management:

   - Move deprecated components to the `Obsolete` folder and file
   - Maintain chronological order in the `GH_ComponentName_OBSOLETE.cs` (newest to oldest)
   - Add version deprecation date in file header comments

### Major Version Changes

When moving to a new major release:

1. Remove all obsolete classes
2. Reset the version number count for current components if necessary

# Grasshopper Component Naming Conventions

## **1. Component Names**

- Use clear, descriptive names in **Pascal Case**.
  - Examples: `Individual From Json`, `Auto Gene Selector`.
- **Action Components:** Use verbs (e.g., `Convert Json`, `Select Genes`).
- **Object Components:** Use nouns (e.g., `Individual`, `Gene`).

## **2. Inputs and Outputs**

- **Inputs:**
  - Use single letters or short names.
  - Examples: `J` (JSON), `SD` (Search Document), `G` (Genes).
- **Outputs:**
  - Be descriptive and concise.
  - Examples: `I` (Individual), `GID` (Gene IDs).

## **3. Categories and Subcategories**

- Organize logically under a **Category > Subcategory**.
  - Examples: - **Category**: `Geospiza` - **Subcategory**: `Converters`
    Use:

```cs
public override GH_Exposure
```

levels to further organize the plugin.

## **4. Consistency**

- Maintain consistent naming patterns across similar components.
  - Example: `Convert Json`, `Convert Individual`.

## **5. Redundancy**

- Avoid redundant information in names.
  - Bad: `Json Tools - Json Convert`
  - Good: `Json Convert`

## Example Component Breakdown

| **Component**          | **Inputs**       | **Outputs**      | **Description**                          |
| ---------------------- | ---------------- | ---------------- | ---------------------------------------- |
| `Individual From Json` | `J` (JSON)       | `I` (Individual) | Converts a JSON string to an individual. |
| `Evaluate Fitness`     | `I` (Individual) | `F` (Fitness)    | Evaluates the fitness of an individual.  |
