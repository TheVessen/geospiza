import adapter from '@sveltejs/adapter-static';

/** @type {import('@sveltejs/kit').Config} */
const config = {
	kit: {
		adapter: adapter({
			pages: '../EmbeddedAssets/web',
			assets: '../EmbeddedAssets/web',
			fallback: 'index.html',
			precompress: false,
			clean: false
		})
	}
};

export default config;
