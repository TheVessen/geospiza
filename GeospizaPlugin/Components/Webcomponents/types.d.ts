export type WebIndividual = {
    Meshes: {
        Indices: number[];
        Vertices: number[];
        Material: Material;
        Fitness: number;
    }[];
};

export type Material = {
    Color: string;
    Metalness: number;
    Roughness: number;
    Opacity: number;
};