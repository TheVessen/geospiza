<script lang="ts">
  import * as THREE from "three";
  import { onMount, onDestroy } from "svelte";
  import type { SolverResponse } from "$lib/types";
  import { clearScene, getThreeMeshData, initThree } from "$lib/three-helpers";
  import { initD3Chart, updateFitnessChart } from "$lib/d3-helpers";
  import { WebSocketService } from "$lib/websocket";

  // Constants
  let wsService: WebSocketService;
  const WS_URL = "ws://127.0.0.1:8181";
  let connectionStatus = $state("disconnected");

  // Solver related variables
  let fitnessValues: number[] = $state([]);
  let fitnessValue = $derived(fitnessValues[fitnessValues.length - 1] || 0);
  const MESH_UPDATE_INTERVAL = 10;

  // Three.js related variables
  let canvas: HTMLCanvasElement;
  let scene: THREE.Scene;

  // D3 related variables
  let chartContainer: HTMLDivElement;

  function handleMessage(data: SolverResponse) {
    fitnessValues = [...fitnessValues, data.fitness];
    if (
      data.meshes?.length > 0 &&
      fitnessValues.length % MESH_UPDATE_INTERVAL === 0
    ) {
      const meshGroup = getThreeMeshData(data.meshes);
      clearScene(scene);
      scene.add(meshGroup);
    }
  }

  /**
   * Command to run the solver.
   */
  function runSolver() {
    fitnessValues = [];
    wsService.runSolver();
  }

  $effect(() => {
    if (fitnessValues.length && chartContainer) {
      updateFitnessChart(chartContainer, fitnessValues);
    }
  });

  onMount(() => {
    wsService = new WebSocketService(WS_URL);

    wsService.on("connected", () => {
      connectionStatus = wsService.connectionStatus;
    });

    wsService.on("message", (data: SolverResponse) => {
      handleMessage(data);
    });

    wsService.on("error", (err) => {
      connectionStatus = wsService.connectionStatus;
    });

    wsService.on("disconnected", () => {
      connectionStatus = wsService.connectionStatus;
    });

    wsService.connect();
    ({ scene } = initThree(canvas));
    initD3Chart(chartContainer);
  });

  onDestroy(() => {
    wsService?.disconnect();
  });
</script>

<main class="bg-gray-900 min-h-screen">
  <section class="max-w-7xl mx-auto p-6 space-y-6">
    <h1 class="text-4xl font-bold text-center text-white mb-8">
      Remote Grasshopper Solver
    </h1>

    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <!-- Status Card -->
      <div
        class="bg-gray-800 rounded-xl shadow-lg p-6 transition-all duration-200
        {connectionStatus === 'connected'
          ? 'ring-2 ring-green-500'
          : connectionStatus === 'connecting'
            ? 'ring-2 ring-amber-500'
            : connectionStatus === 'disconnected'
              ? 'ring-2 ring-pink-500'
              : 'ring-2 ring-red-500'}"
      >
        <div class="flex items-center space-x-2 mb-4">
          <div
            class="w-3 h-3 rounded-full
            {connectionStatus === 'connected'
              ? 'bg-green-500'
              : connectionStatus === 'connecting'
                ? 'bg-amber-500'
                : connectionStatus === 'disconnected'
                  ? 'bg-pink-500'
                  : 'bg-red-500'}"
          ></div>
          <h2 class="text-xl font-semibold text-white">Solver Status</h2>
        </div>
        <div class="space-y-3 text-gray-300">
          <p><span class="font-medium">Status:</span> {connectionStatus}</p>
          <p>
            <span class="font-medium">Current Fitness:</span>
            {fitnessValue || "N/A"}
          </p>
          <p>
            <span class="font-medium">Number of Individuals:</span>
            {fitnessValues.length}
          </p>
        </div>
        <div class="mt-6">
          <button
            class="w-full bg-blue-500 hover:bg-blue-600 text-white font-medium py-3 px-6 rounded-lg
            transition-all duration-200 transform hover:scale-[1.02] active:scale-[0.98]
            disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:scale-100
            focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 focus:ring-offset-gray-800"
            onclick={runSolver}
            disabled={connectionStatus !== "connected"}
          >
            Run Solver
          </button>
        </div>
      </div>

      <!-- 3D Viewer -->
      <div class="bg-gray-800 rounded-xl shadow-lg overflow-hidden">
        <div class="canvas-container w-full h-[400px]">
          <canvas bind:this={canvas} class="w-full h-full"></canvas>
        </div>
      </div>
    </div>

    <!-- Chart Section -->
    <div class="bg-gray-800 rounded-xl shadow-lg p-6">
      <h3 class="text-xl font-semibold mb-4 text-white">
        Fitness Evolution Graph
      </h3>
      <div bind:this={chartContainer} class="w-full h-[400px]"></div>
    </div>
  </section>
</main>
