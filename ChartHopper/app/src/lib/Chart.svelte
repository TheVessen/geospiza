<script lang="ts">
	import { onMount } from "svelte";
	import { renderChart } from "./charts/index";
	import type { ChartRenderConfig } from "./types";

	let { cfg }: { cfg: ChartRenderConfig } = $props();

	let container: HTMLDivElement;

	onMount(() => {
		renderChart(cfg);
	});
</script>

<div class="w-full rounded-lg bg-white/5 p-2" bind:this={container}>
	<div id={cfg.containerId} class="w-full"></div>
</div>
