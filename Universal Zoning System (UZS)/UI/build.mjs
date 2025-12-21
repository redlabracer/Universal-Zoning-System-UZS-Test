import * as esbuild from 'esbuild';
import * as path from 'path';
import { fileURLToPath } from 'url';
import { sassPlugin } from 'esbuild-sass-plugin';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const args = process.argv.slice(2);
const watch = args.includes('--watch');

// Build as ES module for CS2 UI system
// CS2 modding uses externals for engine/React - they're provided by the game
const config = {
    entryPoints: [path.join(__dirname, 'src/index.tsx')],
    bundle: true,
    outfile: path.join(__dirname, 'dist/Universal Zoning System (UZS).mjs'),
    format: 'esm',
    jsx: 'transform',
    jsxFactory: 'React.createElement',
    jsxFragment: 'React.Fragment',
    // CS2 provides these at runtime
    external: [
        'cs2/api',
        'cs2/l10n',
        'cs2/utils',
        'cs2/ui',
        'cs2/modding',
        'react',
        'react-dom'
    ],
    plugins: [
        sassPlugin()
    ],
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
