import React, { useEffect, useMemo, useState } from 'react';
import { bindValue, trigger, useValue } from 'cs2/api';
import { useLocalization } from 'cs2/l10n';
import { getModule } from 'cs2/modding';
import { FocusDisabled } from 'cs2/input';
import { Button, Dropdown, DropdownItem, DropdownToggle, PanelSection, PanelSectionRow, Scrollable, Tooltip } from 'cs2/ui';

const GROUP = 'universalZoning';
const LOC = 'UniversalZoning.CustomZones.';
const PACK_ICON = 'coui://universalzoningsystem/icons/universal_icon.svg';

const Checkbox: React.FC<{ checked?: boolean; disabled?: boolean; onChange?: (value: boolean) => void }> =
    getModule('game-ui/common/input/toggle/checkbox/checkbox.tsx', 'Checkbox');
const TextInput: React.FC<any> = getModule('game-ui/common/input/text/text-input.tsx', 'TextInput');
const textInputTheme = getModule('game-ui/game/components/selected-info-panel/shared-components/text-input/text-input.module.scss', 'classes');
const PanelTitleBar: React.FC<{ children?: React.ReactNode }> =
    getModule('game-ui/common/panel/panel-title-bar.tsx', 'PanelTitleBar');
const DraggablePanel: React.FC<any> = getModule('game-ui/common/panel/draggable-panel/draggable-panel.tsx', 'DraggablePanel');
const iceflakeTheme = getModule('game-ui/common/panel/themes/iceflake-panel.module.scss', 'classes');
const gameDropdownTheme = getModule('game-ui/game/themes/game-dropdown.module.scss', 'classes');
const VanillaButton: React.FC<any> = getModule('game-ui/common/input/button/button.tsx', 'Button');
const dialogButtonTheme = getModule('game-ui/common/input/button/themes/dialog-button.module.scss', 'classes');

interface CustomZone {
    Id: string;
    Name: string;
    BaseZoneType: string;
    SourceZones: string[];
}

interface SourceZone {
    name: string;
    buildings: number;
}

interface SourceGroup {
    zoneType: string;
    zones: SourceZone[];
}

const builderOpen$ = bindValue<boolean>(GROUP, 'builderOpen', false);
const customZones$ = bindValue<string>(GROUP, 'customZones', '[]');
const sources$ = bindValue<string>(GROUP, 'sources', '[]');
const restartRequired$ = bindValue<boolean>(GROUP, 'restartRequired', false);

enum ButtonLocation {
    UniversalModMenu = 0,
    TopLeft = 1,
    TopRight = 2,
}

const buttonLocation$ = bindValue<number>(GROUP, 'buttonLocation', ButtonLocation.UniversalModMenu);

const setBuilderOpen = (open: boolean) => trigger(GROUP, 'setBuilderOpen', open);

const NEW_ZONE_ID = '';

const ZONE_TYPE_LABELS: Record<string, string> = {
    ResidentialLow: 'Low Density Residential',
    ResidentialRow: 'Row Housing',
    ResidentialMedium: 'Medium Density Residential',
    ResidentialHigh: 'High Density Residential',
    ResidentialLowRent: 'Low Rent Housing',
    ResidentialMixed: 'Mixed Housing',
    CommercialLow: 'Low Density Commercial',
    CommercialHigh: 'High Density Commercial',
    OfficeLow: 'Low Density Office',
    OfficeHigh: 'High Density Office',
    Office: 'Office',
};

const parse = <T,>(json: string, fallback: T): T => {
    try {
        return JSON.parse(json) as T;
    } catch {
        return fallback;
    }
};

const PANEL_STYLE: React.CSSProperties = { width: '520rem' };
const LIST_STYLE: React.CSSProperties = { maxHeight: '400rem' };
const RIGHT_STYLE: React.CSSProperties = { display: 'flex', flexDirection: 'row', alignItems: 'center' };
const COUNT_STYLE: React.CSSProperties = { color: 'var(--textColorDim)', marginRight: '12rem' };
const FIELD_STYLE: React.CSSProperties = { width: '260rem' };
const FOOTER_STYLE: React.CSSProperties = { display: 'flex', flexDirection: 'row', padding: '10rem 4rem 4rem' };
const FOOTER_BUTTON_STYLE: React.CSSProperties = { flex: '1 1 0', margin: '0 6rem', color: '#ffffff', fontWeight: 'bold' };
const INPUT_STYLE: React.CSSProperties = {
    width: '100%',
    backgroundColor: 'rgba(6, 10, 16, 0.7)',
    color: 'var(--textColor)',
    border: 'var(--stroke1) solid rgba(255, 255, 255, 0.1)',
};

const MenuButton = ({ location }: { location: ButtonLocation }) => {
    const { translate } = useLocalization();
    const open = useValue(builderOpen$);
    const current = useValue(buttonLocation$);
    if (current !== location) return null;

    return (
        <Tooltip tooltip={translate(LOC + 'BUTTON_TOOLTIP', 'Custom Zone Builder')}>
            <Button variant="floating" selected={open} src={PACK_ICON} onSelect={() => setBuilderOpen(!open)} />
        </Tooltip>
    );
};

const UniversalModMenuButton = () => <MenuButton location={ButtonLocation.UniversalModMenu} />;
const TopLeftMenuButton = () => <MenuButton location={ButtonLocation.TopLeft} />;
const TopRightMenuButton = () => <MenuButton location={ButtonLocation.TopRight} />;

const CustomZoneBuilderPanel = () => {
    const { translate } = useLocalization();
    const open = useValue(builderOpen$);
    const zones = parse<CustomZone[]>(useValue(customZones$), []);
    const sourceGroups = parse<SourceGroup[]>(useValue(sources$), []);
    const restartRequired = useValue(restartRequired$);

    const [selectedId, setSelectedId] = useState<string>(NEW_ZONE_ID);
    const [name, setName] = useState('');
    const [baseType, setBaseType] = useState<string>('');
    const [selectedSources, setSelectedSources] = useState<string[]>([]);

    const selectedZone = zones.find((z) => z.Id === selectedId) ?? null;

    useEffect(() => {
        if (selectedZone) {
            setName(selectedZone.Name);
            setBaseType(selectedZone.BaseZoneType);
            setSelectedSources(selectedZone.SourceZones);
        }
    }, [selectedId, open]);

    useEffect(() => {
        if (!baseType && sourceGroups.length > 0) setBaseType(sourceGroups[0].zoneType);
    }, [sourceGroups.length]);

    const currentGroup = useMemo(() => sourceGroups.find((g) => g.zoneType === baseType), [sourceGroups, baseType]);

    if (!open) return null;

    const totalBuildings = (currentGroup?.zones ?? [])
        .filter((z) => selectedSources.includes(z.name))
        .reduce((sum, z) => sum + z.buildings, 0);

    const startNew = () => {
        setSelectedId(NEW_ZONE_ID);
        setName('');
        setSelectedSources([]);
        if (sourceGroups.length > 0) setBaseType(sourceGroups[0].zoneType);
    };

    const selectZone = (id: string) => (id === NEW_ZONE_ID ? startNew() : setSelectedId(id));

    const setSource = (zoneName: string, checked: boolean) =>
        setSelectedSources((prev) => (checked ? [...prev.filter((n) => n !== zoneName), zoneName] : prev.filter((n) => n !== zoneName)));

    const changeBaseType = (type: string) => {
        setBaseType(type);
        setSelectedSources([]);
    };

    const canSave = name.trim().length > 0 && !!baseType && selectedSources.length > 0;

    const save = () => {
        if (!canSave) return;
        trigger(GROUP, 'saveCustomZone', JSON.stringify({
            id: selectedId || null,
            name: name.trim(),
            baseZoneType: baseType,
            sourceZones: selectedSources,
        }));
    };

    const remove = () => {
        if (!selectedId) return;
        trigger(GROUP, 'deleteCustomZone', selectedId);
        startNew();
    };

    const zoneLabel = (zoneName: string) => translate(`Assets.NAME[${zoneName}]`, zoneName) ?? zoneName;
    const typeLabel = (type: string) => ZONE_TYPE_LABELS[type] ?? type;
    const newZoneLabel = translate(LOC + 'NEW_ZONE', '+ New Zone') ?? '+ New Zone';

    const zoneDropdown = (
        <Dropdown
            theme={gameDropdownTheme}
            content={[
                <DropdownItem key="__new" value={NEW_ZONE_ID} selected={selectedId === NEW_ZONE_ID} onChange={() => selectZone(NEW_ZONE_ID)}>
                    {newZoneLabel}
                </DropdownItem>,
                ...zones.map((z) => (
                    <DropdownItem key={z.Id} value={z.Id} selected={z.Id === selectedId} onChange={() => selectZone(z.Id)}>
                        {z.Name}
                    </DropdownItem>
                )),
            ]}
        >
            <DropdownToggle style={FIELD_STYLE}>{selectedZone ? selectedZone.Name : newZoneLabel}</DropdownToggle>
        </Dropdown>
    );

    const typeField = selectedZone ? (
        typeLabel(baseType)
    ) : (
        <Dropdown
            theme={gameDropdownTheme}
            content={sourceGroups.map((g) => (
                <DropdownItem key={g.zoneType} value={g.zoneType} selected={g.zoneType === baseType} onChange={() => changeBaseType(g.zoneType)}>
                    {typeLabel(g.zoneType)}
                </DropdownItem>
            ))}
        >
            <DropdownToggle style={FIELD_STYLE}>{typeLabel(baseType)}</DropdownToggle>
        </Dropdown>
    );

    const nameInput = (
        <div style={FIELD_STYLE}>
            <TextInput
                type="text"
                value={name}
                maxLength={64}
                className={textInputTheme?.input}
                style={INPUT_STYLE}
                onChange={(e: any) => setName(e.target.value)}
            />
        </div>
    );

    return (
        <DraggablePanel
            initialPosition={{ x: 0.5, y: 0.3 }}
            style={PANEL_STYLE}
            theme={iceflakeTheme}
            header={<PanelTitleBar>{translate(LOC + 'TITLE', 'UZS Zone Composer')}</PanelTitleBar>}
            onClose={() => setBuilderOpen(false)}
        >
            <FocusDisabled>
            <PanelSection>
                <PanelSectionRow left={translate(LOC + 'ZONE', 'Custom Zone')} right={zoneDropdown} />
                <PanelSectionRow left={translate(LOC + 'NAME', 'Name')} right={nameInput} />
                <PanelSectionRow left={translate(LOC + 'BASE_TYPE', 'Zone Type')} right={typeField} />
            </PanelSection>

            <PanelSection>
                <PanelSectionRow
                    uppercase
                    left={translate(LOC + 'SOURCES', 'Source Zones')}
                    right={`${totalBuildings} ${translate(LOC + 'BUILDINGS', 'Buildings')}`}
                />
                <Scrollable style={LIST_STYLE}>
                    {(currentGroup?.zones ?? []).map((z) => (
                        <PanelSectionRow
                            key={z.name}
                            subRow
                            left={zoneLabel(z.name)}
                            right={
                                <div style={RIGHT_STYLE}>
                                    <span style={COUNT_STYLE}>{z.buildings}</span>
                                    <Checkbox checked={selectedSources.includes(z.name)} onChange={(v) => setSource(z.name, v)} />
                                </div>
                            }
                        />
                    ))}
                </Scrollable>
                {restartRequired && <PanelSectionRow left={translate(LOC + 'RESTART_REQUIRED', 'Restart required for some changes.')} />}
            </PanelSection>

            <div style={FOOTER_STYLE}>
                <VanillaButton
                    theme={dialogButtonTheme}
                    className={dialogButtonTheme?.negative}
                    style={FOOTER_BUTTON_STYLE}
                    disabled={!selectedId}
                    onSelect={remove}
                >
                    {translate(LOC + 'DELETE', 'Delete')}
                </VanillaButton>
                <VanillaButton theme={dialogButtonTheme} style={FOOTER_BUTTON_STYLE} disabled={!canSave} onSelect={save}>
                    {translate(LOC + 'SAVE', 'Save')}
                </VanillaButton>
            </div>
            </FocusDisabled>
        </DraggablePanel>
    );
};

export const registerCustomZoneBuilder = (registry: any) => {
    registry.append('UniversalModMenu', UniversalModMenuButton);
    registry.append('GameTopLeft', TopLeftMenuButton);
    registry.append('GameTopRight', TopRightMenuButton);
    registry.append('Game', CustomZoneBuilderPanel);
};
