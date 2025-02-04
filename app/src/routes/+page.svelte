<script lang="ts">
  import * as THREE from "three";
  import { onMount, onDestroy } from "svelte";
  import type { SolverResponse } from "$lib/types";
  import { clearScene, getThreeMeshData, initThree } from "$lib/three-helpers";
  import { initD3Chart, updateFitnessChart } from "$lib/d3-helpers";
  import { WebSocketService } from "$lib/websocket";
  import Button from "$lib/Components/Button.svelte";

  // Constants
  let wsService: WebSocketService;
  const WS_URL = "ws://127.0.0.1:8181";
  let connectionStatus = $state("disconnected");

  // Solver related variables
  let fitnessValues: number[] = $state([]);
  let fitnessValue = $derived(fitnessValues[fitnessValues.length - 1] || 0);
  let currentGeneration = $state(0);
  let isCanceling = $state(false);
  let showMeshPreview = $state(true);
  let solverRunning = $state(false);
  const MESH_UPDATE_INTERVAL = 2;

  // Three.js related variables
  let canvas: HTMLCanvasElement | null = $state(null);
  let scene: THREE.Scene;

  // D3 related variables
  let chartContainer: HTMLDivElement;

  function handleMessage(data: SolverResponse) {
    if (data.status === "canceled") {
      isCanceling = false;
      solverRunning = false;
      return;
    } else if (data.status === "running") {
      solverRunning = true;
      if (!isCanceling) {
        fitnessValues = [...fitnessValues, data.fitness];
        currentGeneration = data.currentGeneration;

        if (
          showMeshPreview &&
          data.meshes?.length > 0 &&
          fitnessValues.length % MESH_UPDATE_INTERVAL === 0
        ) {
          const meshGroup = getThreeMeshData(data.meshes);
          clearScene(scene);
          scene.add(meshGroup);
        }
      }
    } else if (data.status === "done") {
      solverRunning = false;
    }
  }

  function toggleMeshPreview() {
    showMeshPreview = !showMeshPreview;
    if (!showMeshPreview) {
      clearScene(scene);
    }
  }

  async function cancelSolver() {
    isCanceling = true;
    await wsService.stopSolver();
  }

  function runSolver() {
    fitnessValues = [];
    solverRunning = true;
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
    if (canvas) ({ scene } = initThree(canvas));
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
          <p><span class="font-bold">Status:</span> {connectionStatus}</p>
          <p>
            <span class="font-bold">Current Fitness:</span>
            {fitnessValue || "N/A"}
          </p>
          <p>
            <span class="font-bold">Number of Individuals:</span>
            {fitnessValues.length}
          </p>
          <p>
            <span class="font-bold">Current Generation:</span>
            {currentGeneration}
          </p>
        </div>
        <div class="mt-6 space-x-4">
          <Button
            variant="primary"
            onclick={runSolver}
            disabled={connectionStatus !== "connected" || solverRunning}
          >
            Run Solver
          </Button>
          <Button
            variant="danger"
            onclick={cancelSolver}
            disabled={connectionStatus !== "connected" || !solverRunning}
          >
            {isCanceling ? "Canceling..." : "Cancel Solver"}
          </Button>
        </div>

        <div class="mt-10 grid">
          <p class="text-white">Show Mesh Preview</p>
          <button
            class="relative inline-flex h-6 w-11 items-center rounded-full
        {showMeshPreview ? 'bg-blue-500' : 'bg-gray-600'}
        transition-colors duration-200 ease-in-out focus:outline-none"
            onclick={toggleMeshPreview}
            aria-label="Toggle Mesh Preview"
          >
            <span
              class="inline-block h-4 w-4 transform rounded-full bg-white transition duration-200 ease-in-out
          {showMeshPreview ? 'translate-x-6' : 'translate-x-1'}"
            ></span>
          </button>
        </div>
      </div>

      <!-- Update 3D Viewer visibility -->
      <div
        class="bg-gray-800 rounded-xl shadow-lg overflow-hidden {showMeshPreview
          ? 'block'
          : 'hidden'}"
      >
        <div class="canvas-container w-full h-[400px]">
          <canvas bind:this={canvas} class="w-full h-full"></canvas>
        </div>
      </div>

      <!-- Chart Section -->
      <div class="bg-gray-800 rounded-xl shadow-lg p-6 col-span-2">
        <h3 class="text-xl font-semibold mb-4 text-white">
          Fitness Evolution Graph
        </h3>
        <div bind:this={chartContainer} class="w-full h-[400px]"></div>
      </div>
    </div>
  </section>
</main>
