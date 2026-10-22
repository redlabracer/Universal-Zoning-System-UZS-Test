import * as esbuild from 'esbuild';
import * as path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const args = process.argv.slice(2);
const watch = args.includes('--watch');

const gameGlobals = {
    'react': 'React',
    'react-dom': 'ReactDOM',
    'cs2/api': 'cs2/api',
    'cs2/bindings': 'cs2/bindings',
    'cs2/l10n': 'cs2/l10n',
    'cs2/ui': 'cs2/ui',
    'cs2/utils': 'cs2/utils',
    'cs2/input': 'cs2/input',
    'cs2/modding': 'cs2/modding',
};

const gameGlobalsPlugin = {
    name: 'game-globals',
    setup(build) {
        const filter = new RegExp('^(' + Object.keys(gameGlobals).map((k) => k.replace('/', '\\/')).join('|') + ')$');
        build.onResolve({ filter }, (args) => ({ path: args.path, namespace: 'game-global' }));
        build.onLoad({ filter: /.*/, namespace: 'game-global' }, (args) => ({
            contents: `module.exports = window[${JSON.stringify(gameGlobals[args.path])}];`,
            loader: 'js',
        }));
    },
};

// Build as ES module for CS2 UI system
const config = {
    entryPoints: [path.join(__dirname, 'src/index.tsx')],
    bundle: true,
    outfile: path.join(__dirname, 'dist/Universal Zoning System (UZS).mjs'),
    format: 'esm',
    jsx: 'transform',
    jsxFactory: 'React.createElement',
    jsxFragment: 'React.Fragment',
    plugins: [gameGlobalsPlugin],
    minify: !watch,
    sourcemap: watch,
    target: 'es2020',
    define: {
        'process.env.NODE_ENV': watch ? '"development"' : '"production"'
    },
    banner: {
        js: `// Universal Zoning System UI Module for Cities: Skylines II
`
    }
};

if (watch) {
    const ctx = await esbuild.context(config);
    await ctx.watch();
    console.log('Watching for changes...');
} else {
    await esbuild.build(config);
    console.log('Build complete: dist/Universal Zoning System (UZS).mjs');
}
