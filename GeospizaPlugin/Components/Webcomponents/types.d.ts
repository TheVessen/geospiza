export type WebIndividual = {
    Meshes: {
        Indices: number[];
        Vertices: number[];
        Material: Material;
        Fitness: number;
    }[];
    // Additional data declared in grasshopper
};

export type Material = {
    Color: string;
    Metalness: number;
    Roughness: number;
    Opacity: number;
};