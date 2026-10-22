/**
 * Universal Zoning System - UI Module for Cities: Skylines II
 *
 * The universal zones appear as a native category tab in the zone toolbar.
 * The Custom Zone Builder is reachable through the Universal Mod Menu.
 */

import { registerCustomZoneBuilder } from './components/CustomZoneBuilder';

export default function register(registry: any): void {
    try {
        registerCustomZoneBuilder(registry);
        console.log('[UZS] UI module loaded');
    } catch (e) {
        console.error('[UZS] UI registration failed', e);
    }
}

export const hasCSS = false;
