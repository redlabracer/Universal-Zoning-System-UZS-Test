import { trigger, call } from "cs2/api";
import { useState, useEffect, useCallback } from "react";

const BINDING_GROUP = "universalZoningDistrict";

interface DistrictSettings {
    EnableNorthAmerica: boolean;
    EnableEuropean: boolean;
    EnableUnitedKingdom: boolean;
    EnableGermany: boolean;
    EnableFrance: boolean;
    EnableNetherlands: boolean;
    EnableEasternEurope: boolean;
    EnableJapan: boolean;
    EnableChina: boolean;
    EnableDetached: boolean;
    EnableAttached: boolean;
    EnableMixed: boolean;
}

const defaultSettings: DistrictSettings = {
    EnableNorthAmerica: true,
    EnableEuropean: true,
    EnableUnitedKingdom: true,
    EnableGermany: true,
    EnableFrance: true,
    EnableNetherlands: true,
    EnableEasternEurope: true,
    EnableJapan: true,
    EnableChina: true,
    EnableDetached: true,
    EnableAttached: true,
    EnableMixed: true,
};

const regionOptions = [
    { key: "NA", label: "North America", settingKey: "EnableNorthAmerica" as keyof DistrictSettings },
    { key: "EU", label: "European", settingKey: "EnableEuropean" as keyof DistrictSettings },
    { key: "UK", label: "United Kingdom", settingKey: "EnableUnitedKingdom" as keyof DistrictSettings },
    { key: "GER", label: "Germany", settingKey: "EnableGermany" as keyof DistrictSettings },
    { key: "FR", label: "France", settingKey: "EnableFrance" as keyof DistrictSettings },
    { key: "NL", label: "Netherlands", settingKey: "EnableNetherlands" as keyof DistrictSettings },
    { key: "EE", label: "Eastern Europe", settingKey: "EnableEasternEurope" as keyof DistrictSettings },
    { key: "JP", label: "Japan", settingKey: "EnableJapan" as keyof DistrictSettings },
    { key: "CN", label: "China", settingKey: "EnableChina" as keyof DistrictSettings },
];

const typeOptions = [
    { key: "Detached", label: "Detached Houses", settingKey: "EnableDetached" as keyof DistrictSettings },
    { key: "Attached", label: "Attached/Row Houses", settingKey: "EnableAttached" as keyof DistrictSettings },
    { key: "Mixed", label: "Mixed Use", settingKey: "EnableMixed" as keyof DistrictSettings },
];

interface Entity {
    index: number;
    version: number;
}

export const DistrictSettingsPanel = ({ entity }: { entity: Entity }) => {
    const [settings, setSettings] = useState<DistrictSettings>(defaultSettings);
    const [isExpanded, setIsExpanded] = useState(false);
    const [isLoading, setIsLoading] = useState(false);

    useEffect(() => {
        if (entity && entity.index !== 0) {
            setIsLoading(true);
            call(BINDING_GROUP, "getDistrictSettings", entity)
                .then((result: string) => {
                    setSettings(JSON.parse(result));
                })
                .catch((err: unknown) => {
                    console.error("[UZS] Failed:", err);
                })
                .finally(() => {
                    setIsLoading(false);
                });
        }
    }, [entity?.index, entity?.version]);

    const handleToggle = useCallback((key: string, settingKey: keyof DistrictSettings) => {
        if (!entity || entity.index === 0) return;
        const newValue = !settings[settingKey];
        setSettings(prev => ({ ...prev, [settingKey]: newValue }));
        trigger(BINDING_GROUP, "setDistrictSetting", entity, key, newValue);
    }, [entity, settings]);

    if (!entity || entity.index === 0) return null;

    return (
        <div className="uzs-district-settings">
            <div className="uzs-district-header" onClick={() => setIsExpanded(!isExpanded)}>
                <span className="uzs-district-title">Universal Zoning Settings</span>
                <span className={"uzs-expand-icon " + (isExpanded ? "expanded" : "")}>V</span>
            </div>
            {isExpanded && (
                <div className="uzs-district-content">
                    {isLoading ? (
                        <div>Loading...</div>
                    ) : (
                        <div>
                            <div className="uzs-section">
                                <div className="uzs-section-title">Allowed Regions</div>
                                <div className="uzs-options-grid">
                                    {regionOptions.map(opt => (
                                        <label key={opt.key} className="uzs-checkbox-label">
                                            <input
                                                type="checkbox"
                                                checked={settings[opt.settingKey]}
                                                onChange={() => handleToggle(opt.key, opt.settingKey)}
                                            />
                                            <span>{opt.label}</span>
                                        </label>
                                    ))}
                                </div>
                            </div>
                            <div className="uzs-section">
                                <div className="uzs-section-title">Allowed Building Types</div>
                                <div className="uzs-options-grid">
                                    {typeOptions.map(opt => (
                                        <label key={opt.key} className="uzs-checkbox-label">
                                            <input
                                                type="checkbox"
                                                checked={settings[opt.settingKey]}
                                                onChange={() => handleToggle(opt.key, opt.settingKey)}
                                            />
                                            <span>{opt.label}</span>
                                        </label>
                                    ))}
                                </div>
                            </div>
                        </div>
                    )}
                </div>
            )}
        </div>
    );
};

export default DistrictSettingsPanel;
