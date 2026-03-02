import { sveltekit } from '@sveltejs/kit/vite';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'vite';

export default defineConfig({
	plugins: [tailwindcss(), sveltekit()],
	build: {
		// MSBuild holds the output directory open during C# compilation, so we
		// let the .csproj clean it with a RemoveDir task before npm runs instead.
		emptyOutDir: false
	}
});
