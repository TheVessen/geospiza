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

type BaseResponse = {
  status: "running" | "canceled" | "done";
};

type RunningSolverResponse = {
  status: "running";
  meshes: MeshData[];
  fitness: number;
  currentGeneration: number;
};

type CompletedSolverResponse = {
  status: "done";
  finalFitness?: number;
};

type CanceledSolverResponse = {
  status: "canceled";
};

export type SolverResponse =
  | RunningSolverResponse
  | CompletedSolverResponse
  | CanceledSolverResponse;