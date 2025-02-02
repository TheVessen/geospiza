export type Material = {
  Color: string;
  Metalness: number;
  Roughness: number;
  Opacity: number;
};

export type MeshData = {
  Indices: number[];
  Vertices: number[];
  Material: Material;
}

export type SolverResponse = {
  meshes: MeshData[];
  fitness: number;
};

