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

// Zone lists longer than this start with their region groups collapsed.
const AUTO_EXPAND_LIMIT = 15;
const OTHER_GROUP = '';

interface SourceRow extends SourceZone {
    label: string;
}

interface RegionGroup {
    key: string;
    rows: SourceRow[];
}

// Mod and vanilla zone names start with a region code such as "EU", "NA" or "USA".
const regionOf = (label: string) => /^([A-Z]{2,4})(?=\s)/.exec(label)?.[1] ?? OTHER_GROUP;

const PANEL_STYLE: React.CSSProperties = { width: '520rem' };
const LIST_STYLE: React.CSSProperties = { maxHeight: '400rem' };
const COUNT_STYLE: React.CSSProperties = { color: 'var(--textColorDim)', marginRight: '12rem' };
const TOOLBAR_STYLE: React.CSSProperties = { display: 'flex', flexDirection: 'row', alignItems: 'center', padding: '6rem 8rem' };
const SEARCH_STYLE: React.CSSProperties = { flex: '1 1 0' };
const SMALL_BUTTON_STYLE: React.CSSProperties = {
    marginLeft: '8rem',
    padding: '4rem 10rem',
    borderRadius: '4rem',
    backgroundColor: 'rgba(255, 255, 255, 0.1)',
    color: 'var(--textColor)',
    fontSize: 'var(--fontSizeS)',
    cursor: 'pointer',
};
const GROUP_HEADER_STYLE: React.CSSProperties = {
    display: 'flex',
    flexDirection: 'row',
    alignItems: 'center',
    padding: '6rem 8rem',
    marginTop: '2rem',
    backgroundColor: 'rgba(255, 255, 255, 0.06)',
    borderRadius: '4rem',
};
const GROUP_TITLE_STYLE: React.CSSProperties = {
    flex: '1 1 0',
    display: 'flex',
    flexDirection: 'row',
    alignItems: 'center',
    color: 'var(--textColor)',
    fontWeight: 'bold',
    cursor: 'pointer',
};
const GROUP_COUNT_STYLE: React.CSSProperties = { color: 'var(--textColorDim)', fontWeight: 'normal', marginLeft: '8rem' };
const ROW_STYLE: React.CSSProperties = { display: 'flex', flexDirection: 'row', alignItems: 'center', padding: '4rem 8rem' };
const NESTED_ROW_STYLE: React.CSSProperties = { ...ROW_STYLE, paddingLeft: '26rem' };
const ROW_LABEL_STYLE: React.CSSProperties = {
    flex: '1 1 0',
    color: 'var(--textColor)',
    fontSize: 'var(--fontSizeS)',
    cursor: 'pointer',
    marginRight: '8rem',
};
const EMPTY_STYLE: React.CSSProperties = { padding: '8rem', color: 'var(--textColorDim)', fontSize: 'var(--fontSizeS)' };
const chevronStyle = (expanded: boolean): React.CSSProperties => ({
    width: '0',
    height: '0',
    borderTop: '5rem solid transparent',
    borderBottom: '5rem solid transparent',
    borderLeft: '7rem solid var(--textColor)',
    marginRight: '10rem',
    transform: expanded ? 'rotate(90deg)' : 'none',
});
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
    const [search, setSearch] = useState('');
    const [expanded, setExpanded] = useState<Record<string, boolean>>({});

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

    const setSources = (zoneNames: string[], checked: boolean) =>
        setSelectedSources((prev) => {
            const rest = prev.filter((n) => !zoneNames.includes(n));
            return checked ? [...rest, ...zoneNames] : rest;
        });

    const changeBaseType = (type: string) => {
        setBaseType(type);
        setSelectedSources([]);
        setSearch('');
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
    const otherLabel = translate(LOC + 'OTHER_REGION', 'Other') ?? 'Other';

    const allRows: SourceRow[] = (currentGroup?.zones ?? [])
        .map((z) => ({ ...z, label: zoneLabel(z.name) }))
        .sort((a, b) => a.label.localeCompare(b.label));

    const query = search.trim().toLowerCase();
    const visibleRows = query
        ? allRows.filter((r) => r.label.toLowerCase().includes(query) || r.name.toLowerCase().includes(query))
        : allRows;
    const visibleNames = visibleRows.map((r) => r.name);

    const regionGroups: RegionGroup[] = [];
    for (const row of visibleRows) {
        const key = regionOf(row.label);
        let group = regionGroups.find((g) => g.key === key);
        if (!group) regionGroups.push((group = { key, rows: [] }));
        group.rows.push(row);
    }
    regionGroups.sort((a, b) => (a.key === OTHER_GROUP ? 1 : b.key === OTHER_GROUP ? -1 : a.key.localeCompare(b.key)));

    const isExpanded = (key: string) => !!query || (expanded[key] ?? allRows.length <= AUTO_EXPAND_LIMIT);
    const toggleGroup = (key: string) => setExpanded((prev) => ({ ...prev, [key]: !isExpanded(key) }));

    const renderRow = (row: SourceRow, nested: boolean) => {
        const checked = selectedSources.includes(row.name);
        return (
            <div key={row.name} style={nested ? NESTED_ROW_STYLE : ROW_STYLE}>
                <div style={ROW_LABEL_STYLE} onClick={() => setSource(row.name, !checked)}>
                    {row.label}
                </div>
                <span style={COUNT_STYLE}>{row.buildings}</span>
                <Checkbox checked={checked} onChange={(v) => setSource(row.name, v)} />
            </div>
        );
    };

    const renderGroup = (group: RegionGroup) => {
        const names = group.rows.map((r) => r.name);
        const selectedCount = names.filter((n) => selectedSources.includes(n)).length;
        const buildings = group.rows.reduce((sum, r) => sum + r.buildings, 0);
        const open = isExpanded(group.key);
        return (
            <div key={group.key || '__other'}>
                <div style={GROUP_HEADER_STYLE}>
                    <div style={GROUP_TITLE_STYLE} onClick={() => toggleGroup(group.key)}>
                        <div style={chevronStyle(open)} />
                        <span>{group.key || otherLabel}</span>
                        <span style={GROUP_COUNT_STYLE}>{`${selectedCount}/${names.length}`}</span>
                    </div>
                    <span style={COUNT_STYLE}>{buildings}</span>
                    <Checkbox checked={selectedCount === names.length} onChange={(v) => setSources(names, v)} />
                </div>
                {open && group.rows.map((r) => renderRow(r, true))}
            </div>
        );
    };

    const sourceList =
        visibleRows.length === 0 ? (
            <div style={EMPTY_STYLE}>{translate(LOC + 'NO_MATCHES', 'No matching zones.')}</div>
        ) : regionGroups.length > 1 ? (
            regionGroups.map(renderGroup)
        ) : (
            visibleRows.map((r) => renderRow(r, false))
        );

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
                <div style={TOOLBAR_STYLE}>
                    <div style={SEARCH_STYLE}>
                        <TextInput
                            type="text"
                            value={search}
                            maxLength={64}
                            placeholder={translate(LOC + 'SEARCH', 'Search zones...')}
                            className={textInputTheme?.input}
                            style={INPUT_STYLE}
                            onChange={(e: any) => setSearch(e.target.value)}
                        />
                    </div>
                    <div style={SMALL_BUTTON_STYLE} onClick={() => setSources(visibleNames, true)}>
                        {translate(LOC + 'SELECT_ALL', 'Select all')}
                    </div>
                    <div style={SMALL_BUTTON_STYLE} onClick={() => setSources(visibleNames, false)}>
                        {translate(LOC + 'CLEAR', 'Clear')}
                    </div>
                </div>
                <Scrollable style={LIST_STYLE}>{sourceList}</Scrollable>
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
