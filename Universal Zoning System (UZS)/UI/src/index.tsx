/**
 * Universal Zoning System - UI Module for Cities: Skylines II
 * 
 * NOTE: District-specific UI integration is complex in CS2.
 * For now, this module provides basic logging.
 * District settings are available via the C# bindings for future UI development.
 */

import { ModRegistrar } from "cs2/modding";
import "./styles/districtSettings.scss";

// Register the mod UI components
const register: ModRegistrar = (moduleRegistry) => {
    console.log('[UZS] UI module loaded');
    console.log('[UZS] District settings backend is ready - UI integration pending');
    
    // The C# backend exposes these bindings:
    // - universalZoningDistrict.getDistrictSettings(entity) -> returns JSON settings
    // - universalZoningDistrict.setDistrictSetting(entity, key, value) -> updates a setting
    // 
    // Future: Integrate with district info panel when CS2 modding API stabilizes
};

export default register;

export const hasCSS = true;
