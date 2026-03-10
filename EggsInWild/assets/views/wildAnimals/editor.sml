<frame layout="1260px 820px"
       background={@Mods/StardewUI/Sprites/MenuBackground}
       border={@Mods/StardewUI/Sprites/MenuBorder}
       border-thickness="36, 36, 40, 36">
    <lane layout="stretch stretch" orientation="vertical" padding="24">
        <lane layout="stretch content" orientation="vertical">
            <banner text="Wild Animals Editor" />
            <label text={HeaderSummary} />
            <label text={StatsSummary} />
            <label text={VisibilitySummary} />
            <lane layout="stretch content" orientation="horizontal">
                <lane layout="340px content" orientation="vertical">
                    <label text="Search" />
                    <textinput layout="320px 54px" text={<>SearchText} />
                </lane>
                <lane layout="stretch content" orientation="vertical" margin="24,0,0,0">
                    <label text="Status" />
                    <label text={DirtyStatusText} />
                </lane>
            </lane>
            <lane layout="stretch content" orientation="horizontal" margin="0,8,0,0">
                <checkbox label-text="Overridden" is-checked={<>FilterOverridden} />
                <checkbox label-text="Disabled" is-checked={<>FilterDisabled} />
                <checkbox label-text="Magic" is-checked={<>FilterMagic} />
                <checkbox label-text="Warnings" is-checked={<>FilterWarnings} />
                <checkbox label-text="Show invalid groups" is-checked={<>ShowInvalidAnimals} />
            </lane>
            <lane layout="stretch content" orientation="horizontal" margin="0,8,0,0">
                <button text="Save" click=|Save()| />
                <button text="Discard" click=|Discard()| />
                <button text="Reload" click=|Reload()| />
            </lane>
            <label text={StatusText} margin="0,8,0,0" />
            <lane *if={ShowSelectionConfirmation} layout="stretch content" orientation="vertical">
                <label text="Unsaved changes detected. Confirm before switching animals." />
                <lane orientation="horizontal">
                    <button text="Switch anyway" click=|ConfirmSelectionChange()| />
                    <button text="Stay here" click=|CancelSelectionChange()| />
                </lane>
            </lane>
            <lane *if={ShowCloseConfirmation} layout="stretch content" orientation="vertical">
                <label text="Unsaved changes detected. Close the menu again to leave, or cancel here." />
                <lane orientation="horizontal">
                    <button text="Arm close" click=|ConfirmClose()| />
                    <button text="Keep editing" click=|CancelClose()| />
                </lane>
            </lane>
        </lane>
        <lane layout="stretch stretch" orientation="horizontal" margin="0,16,0,0">
            <scrollable layout="400px stretch" peeking="32">
                <lane layout="stretch content" orientation="vertical">
                    <label text="Animals" />
                    <label *if={HasHiddenSelection} text="Selected animal is hidden by the current filters." />
                    <label *if={ShowEmptyState} text="No animals match the current search and filters." />
                    <expander *repeat={:AnimalGroups} layout="stretch content" is-expanded="true">
                        <label *outlet="header" text={:Title} />
                        <lane orientation="vertical" margin="16,8,0,8">
                            <lane *repeat={:Animals} orientation="vertical">
                                <lane layout="stretch content" orientation="horizontal" margin="0,0,0,8">
                                    <image *if={HasPreviewSprite} layout="56px 56px" sprite={:PreviewSprite} />
                                    <lane layout="280px content" orientation="vertical" margin="12,0,20,0">
                                        <button layout="stretch content"
                                                text={ButtonText}
                                                click=|OpenAnimal()| />
                                        <label text={MetaText} />
                                        <lane orientation="horizontal">
                                            <label *repeat={:Badges} text={:Text} />
                                        </lane>
                                    </lane>
                                </lane>
                            </lane>
                        </lane>
                    </expander>
                </lane>
            </scrollable>
            <lane layout="stretch stretch" orientation="vertical" margin="72,0,0,0">
                <lane *if={HasSelectedAnimal} *context={SelectedAnimal} layout="stretch content" orientation="vertical">
                    <lane layout="stretch content" orientation="vertical">
                        <label font="dialogue" text={Key} />
                        <lane layout="stretch content" orientation="horizontal" margin="0,12,0,0">
                            <lane layout="180px content" orientation="vertical">
                                <image *if={HasPreviewSprite} layout="144px 144px" sprite={PreviewSprite} />
                                <label *if={MissingPreviewSprite} text="No preview sprite available." />
                            </lane>
                            <lane layout="stretch content" orientation="vertical" margin="28,0,0,0">
                                <lane layout="stretch content" orientation="horizontal">
                                    <label layout="170px content" text="Status:" />
                                    <label layout="stretch content" text={HeaderStatus} />
                                </lane>
                                <lane layout="stretch content" orientation="horizontal">
                                    <label layout="170px content" text="Draft:" />
                                    <label layout="stretch content" text={DirtySummary} />
                                </lane>
                                <lane layout="stretch content" orientation="horizontal">
                                    <label layout="170px content" text="Preview:" />
                                    <label layout="stretch content" text={PreviewStatus} />
                                </lane>
                                <lane layout="stretch content" orientation="horizontal">
                                    <label layout="170px content" text="Enabled:" />
                                    <label layout="stretch content" text={EnabledText} />
                                </lane>
                                <lane layout="stretch content" orientation="horizontal">
                                    <label layout="170px content" text="Large Egg:" />
                                    <label layout="stretch content" text={LargeEggText} />
                                </lane>
                                <lane layout="stretch content" orientation="horizontal">
                                    <label layout="170px content" text="Magic:" />
                                    <label layout="stretch content" text={MagicText} />
                                </lane>
                                <lane layout="stretch content" orientation="horizontal">
                                    <label layout="170px content" text="Price:" />
                                    <label layout="stretch content" text={PriceText} />
                                </lane>
                            </lane>
                        </lane>
                    </lane>
                    <lane layout="stretch stretch" orientation="horizontal" margin="0,16,0,0">
                        <lane layout="190px content" orientation="vertical">
                            <button layout="170px content" text={OverviewTabText} click=|ShowOverviewTab()| />
                            <button layout="170px content" text={LocationTypesTabText} click=|ShowLocationTypesTab()| />
                            <button layout="170px content" text={ModKeysTabText} click=|ShowModKeysTab()| />
                            <button layout="170px content" text={WarningsTabText} click=|ShowWarningsTab()| />
                        </lane>
                        <frame layout="stretch stretch"
                               margin="16,0,0,0"
                               padding="20,16"
                               background={@Mods/StardewUI/Sprites/ControlBorder}>
                            <lane *if={IsOverviewTab} orientation="vertical">
                                <label text="Overview" />
                                <lane layout="stretch content" orientation="horizontal" margin="0,8,0,0">
                                    <label layout="140px content" text="Preview:" />
                                    <label layout="stretch content" text={PreviewStatus} />
                                </lane>
                                <lane layout="stretch content" orientation="horizontal">
                                    <label layout="140px content" text="Saved State:" />
                                    <label layout="stretch content" text={DirtySummary} />
                                </lane>
                                <label text={ScalarSectionLabel} margin="0,12,0,0" />
                                <lane orientation="horizontal" margin="0,8,0,0">
                                    <checkbox label-text="Enabled" is-checked={<>Enabled} />
                                    <button text="Reset" click=|ResetEnabled()| />
                                </lane>
                                <label text={EnabledComparison} />
                                <lane orientation="horizontal">
                                    <checkbox label-text="Large egg" is-checked={<>LargeEgg} />
                                    <button text="Reset" click=|ResetLargeEgg()| />
                                </lane>
                                <label text={LargeEggComparison} />
                                <lane orientation="horizontal">
                                    <checkbox label-text="Magic egg" is-checked={<>IsMagic} />
                                    <button text="Reset" click=|ResetIsMagic()| />
                                </lane>
                                <label text={MagicComparison} />
                                <lane orientation="vertical">
                                    <label text="Price" />
                                    <lane orientation="horizontal">
                                        <textinput layout="220px 54px" text={<>PriceText} />
                                        <button text="Reset" click=|ResetPrice()| />
                                    </lane>
                                    <label text={PriceComparison} />
                                    <label *if={HasValidationErrors} text={PriceError} />
                                </lane>
                                <lane orientation="vertical" margin="0,12,0,0">
                                    <label text="Current values" />
                                    <lane layout="stretch content" orientation="horizontal">
                                        <label layout="140px content" text="Enabled:" />
                                        <label layout="stretch content" text={EnabledText} />
                                    </lane>
                                    <lane layout="stretch content" orientation="horizontal">
                                        <label layout="140px content" text="Large Egg:" />
                                        <label layout="stretch content" text={LargeEggText} />
                                    </lane>
                                    <lane layout="stretch content" orientation="horizontal">
                                        <label layout="140px content" text="Magic:" />
                                        <label layout="stretch content" text={MagicText} />
                                    </lane>
                                    <lane layout="stretch content" orientation="horizontal">
                                        <label layout="140px content" text="Price:" />
                                        <label layout="stretch content" text={PriceText} />
                                    </lane>
                                </lane>
                            </lane>

                            <lane *if={IsLocationTypesTab} orientation="vertical">
                                <label text={LocationTypesSectionLabel} />
                                <lane *repeat={LocationTypes} orientation="vertical" margin="0,0,0,8">
                                    <checkbox label-text={Label} is-checked={<>Allowed} />
                                </lane>
                            </lane>

                            <lane *if={IsModKeysTab} orientation="vertical">
                                <label text={ModGroupsSectionLabel} />
                                <label text="Only mod-group editing is shown here." />
                                <lane *repeat={ModGroups} orientation="vertical" margin="0,0,0,16">
                                    <label font="dialogue" text={ModId} />
                                    <label text={BaseSummary} />
                                    <label text={EffectiveLabel} />
                                    <textinput layout="stretch 54px" text={<>KeysText} />
                                    <label text={WarningText} />
                                    <lane orientation="horizontal">
                                        <button text="Reset to base" click=|ResetToBase()| />
                                        <button text="Clear override" click=|ClearOverride()| />
                                        <button text="Remove group" click=|RemoveGroup()| />
                                    </lane>
                                </lane>
                                <lane orientation="vertical">
                                    <label text="Add custom mod group" />
                                    <lane orientation="horizontal">
                                        <textinput layout="stretch 54px" text={<>NewModGroupId} />
                                        <button text="Add" click=|AddModGroup()| />
                                    </lane>
                                </lane>
                            </lane>

                            <lane *if={IsWarningsTab} orientation="vertical">
                                <label text="Warnings" />
                                <label text={WarningSummary} />
                            </lane>
                        </frame>
                    </lane>
                </lane>
            </lane>
        </lane>
    </lane>
</frame>
