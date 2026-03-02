<script lang="ts">
	import { onMount } from "svelte";
	import Chart from "$lib/Chart.svelte";
	import { getSessionId, fetchSession } from "$lib/session";
	import type { DashboardConfig } from "$lib/types";

	let dashboard = $state<DashboardConfig | null>(null);
	let error = $state<string | null>(null);
	let activeTab = $state(0);

	onMount(async () => {
		const sessionId = getSessionId();
		if (!sessionId) {
			error = "No session ID in URL. Open this page via ChartHopper.";
			return;
		}
		try {
			dashboard = await fetchSession(sessionId);
		} catch (e) {
			error = e instanceof Error ? e.message : "Failed to load chart data.";
		}
	});
</script>

<svelte:head>
	<title>{dashboard?.tabs[0]?.name ?? "ChartHopper"}</title>
</svelte:head>

<div class="min-h-screen bg-[#0f1117] text-gray-100 font-sans">
	{#if error}
		<div class="flex h-screen items-center justify-center">
			<p class="text-red-400 text-sm">{error}</p>
		</div>
	{:else if !dashboard}
		<div class="flex h-screen items-center justify-center">
			<p class="text-gray-500 text-sm animate-pulse">Loading…</p>
		</div>
	{:else}
		<!-- Tab bar (hidden when only one tab) -->
		{#if dashboard.tabs.length > 1}
			<nav class="flex gap-1 border-b border-white/10 px-4 pt-3">
				{#each dashboard.tabs as tab, i}
					<button
						class="px-4 py-2 text-sm rounded-t transition-colors
							{activeTab === i
								? 'border-b-2 border-blue-400 text-blue-300 font-medium'
								: 'text-gray-400 hover:text-gray-200'}"
						onclick={() => (activeTab = i)}
					>
						{tab.name}
					</button>
				{/each}
			</nav>
		{/if}

		<!-- Tab panels -->
		{#each dashboard.tabs as tab, i}
			<section class="p-4 grid gap-4" class:hidden={activeTab !== i}>
				{#each tab.charts as chart}
					<Chart cfg={chart} />
				{/each}
			</section>
		{/each}
	{/if}
</div>
