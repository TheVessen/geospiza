import * as THREE from "three";
import { OrbitControls } from "three/examples/jsm/Addons.js";
import type { Color, MeshData } from "./types";


export function initThree(canvas: HTMLCanvasElement) {
  // Basic renderer setup
  const renderer = new THREE.WebGLRenderer({
    canvas,
    antialias: true
  });

  // Scene setup
  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0xf5f5f5);  // whitesmoke

  const getParentDimensions = () => {
    const parent = canvas.parentElement;
    return {
      width: parent?.clientWidth ?? window.innerWidth,
      height: parent?.clientHeight ?? window.innerHeight
    };
  };

  const { width, height } = getParentDimensions();

  // Camera setup
  const camera = new THREE.PerspectiveCamera(
    60,
    width / height,
    0.1,
    1000
  );
  camera.position.set(5, 5, 5);
  camera.lookAt(0, 0, 0);

  // Basic lighting
  const light = new THREE.DirectionalLight(0xffffff, 20);
  light.position.set(5, 10, 5);
  scene.add(light);
  scene.add(new THREE.AmbientLight(0xffffff, 0.5));

  // Controls
  const controls = new OrbitControls(camera, canvas);

  // Handle resize
  function setSize() {
    if (!canvas.parentElement) return;
    renderer.setSize(canvas.parentElement.clientWidth, canvas.parentElement.clientHeight);
    camera.aspect = canvas.parentElement.clientWidth / canvas.parentElement.clientHeight;
    camera.updateProjectionMatrix();
  }

  setSize();
  window.addEventListener("resize", setSize);

  // Animation loop
  (function animate() {
    requestAnimationFrame(animate);
    controls.update();
    renderer.render(scene, camera);
  })();

  return { scene, camera, renderer, controls };
}

/**
 * Converts an array of MeshData objects into a THREE.Group object containing all meshes.
 * @param webIndividual 
 * @returns 
 */
export function getThreeMeshData(webIndividual: MeshData[]): THREE.Group<THREE.Object3DEventMap> {
  const group = new THREE.Group();

  webIndividual.forEach((meshData) => {
    const vertices = new Float32Array(meshData.vertices);
    const faceIndices = meshData.indices;
    scaleAndRotateVertices(vertices, 0.001);
    const color = rgbStringToThreeColor(meshData.material.color);
    const mesh = VerticesToThreeMesh(vertices, faceIndices);

    mesh.material = new THREE.MeshStandardMaterial({
      color: color,
      roughness: 0.7,
      side: THREE.DoubleSide
    });

    group.add(mesh);
  });

  return group;
}

/**
 * Clears the given THREE.Scene by removing all meshes and disposing of associated resources.
 * @param scene - The THREE.Scene to clear.
 */
export function clearScene(scene: THREE.Scene): void {
  const objectsToRemove: THREE.Object3D[] = [];

  // Traverse the scene and find all objects except lights
  scene.traverse((child: THREE.Object3D) => {
    if (!(child instanceof THREE.Light)) {
      objectsToRemove.push(child);
    }
  });

  objectsToRemove.forEach((object: THREE.Object3D) => {
    // Dispose of geometries and materials if it's a mesh
    if (object instanceof THREE.Mesh) {
      if (object.geometry) {
        object.geometry.dispose();
      }
      if (object.material) {
        if (Array.isArray(object.material)) {
          object.material.forEach((material: THREE.Material) =>
            material.dispose(),
          );
        } else {
          object.material.dispose();
        }
      }
    }

    // Remove from parent or scene
    if (object.parent) {
      object.parent.remove(object);
    } else {
      scene.remove(object);
    }
  });
}


/**
 * Converts an array of vertices and indices into a THREE.Mesh object.
 *
 * @param vertices - The array of vertices.
 * @param indices - The array of indices.
 * @returns The THREE.Mesh object representing the vertices and indices.
 */
function VerticesToThreeMesh(
  vertices: number[] | Float32Array,
  indices: number[] | Uint32Array,
): THREE.Mesh {
  const floatVertices = new Float32Array(vertices);
  const floatFaceIndices = new Uint32Array(indices);
  const geometry = new THREE.BufferGeometry();
  geometry.setAttribute(
    "position",
    new THREE.BufferAttribute(floatVertices, 3),
  );
  geometry.setIndex(new THREE.BufferAttribute(floatFaceIndices, 1)); // Use faces as indices
  geometry.computeVertexNormals(); // This will compute the normals if not provided

  const material = new THREE.MeshBasicMaterial({});

  const mesh = new THREE.Mesh(geometry, material);
  return mesh;
}

/**
 * Scales and rotates the vertices of a mesh based on the given scale factor. (Adjust for Z-up coordinate system)
 * 
 * @param vertices - The vertices of the mesh as a Float32Array.
 * @param scaleFactor - The scale factor to apply to the vertices.
 */
function scaleAndRotateVertices(vertices: Float32Array | undefined, scaleFactor: number): void {
  if (vertices === undefined || vertices.length === 0) {
    console.error("Vertices array is undefined or empty.");
    return;
  }

  const cosTheta = Math.cos(-Math.PI / 2);
  const sinTheta = Math.sin(-Math.PI / 2);

  for (let i = 0; i < vertices.length; i += 3) {
    if (i + 2 < vertices.length) {
      const x = vertices[i];
      const y = vertices[i + 1];
      const z = vertices[i + 2];

      if (x !== undefined && y !== undefined && z !== undefined) {
        vertices[i] = x * scaleFactor;
        vertices[i + 1] = (y * cosTheta - z * sinTheta) * scaleFactor;
        vertices[i + 2] = (y * sinTheta + z * cosTheta) * scaleFactor;
      } else {
        console.warn(`Undefined vertex data at index ${i}`);
      }
    } else {
      console.warn(`Incomplete vertex data at index ${i}`);
    }
  }
}

function rgbStringToThreeColor(color: Color): THREE.Color {
  // Get hex string without alpha channel (remove first 2 chars 'ff')
  const hex = color.name.substring(2);
  return new THREE.Color(`#${hex}`);
}