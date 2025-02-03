export type Color = {
  r: number;
  g: number;
  b: number;
  a: number;
  isKnownColor: boolean;
  isEmpty: boolean;
  isNamedColor: boolean;
  isSystemColor: boolean;
  name: string;
};

export type Material = {
  color: Color;
  metalness: number;
  roughness: number;
  opacity: number;
};

export type MeshData = {
  vertices: number[];
  indices: number[];
  material: Material;
}

export type SolverResponse = {
  meshes: MeshData[];
  fitness: number;
  currentGeneration: number;
};