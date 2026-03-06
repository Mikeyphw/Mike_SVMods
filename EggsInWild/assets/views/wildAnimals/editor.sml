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
            <lane layout="stretch content">
                <lane orientation="vertical">
                    <label text="Search" />
                    <textinput layout="320px 54px" text={<>SearchText} />
                </lane>
                <lane orientation="vertical">
                    <label text="Status" />
                    <label text={DirtyStatusText} />
                </lane>
            </lane>
            <lane layout="stretch content">
                <checkbox label-text="Overridden" is-checked={<>FilterOverridden} />
                <checkbox label-text="Disabled" is-checked={<>FilterDisabled} />
                <checkbox label-text="Magic" is-checked={<>FilterMagic} />
                <checkbox label-text="Warnings" is-checked={<>FilterWarnings} />
                <checkbox label-text="Show invalid groups" is-checked={<>ShowInvalidAnimals} />
            </lane>
            <lane layout="stretch content">
                <button text="Save" click=|Save()| />
                <button text="Discard" click=|Discard()| />
                <button text="Reload" click=|Reload()| />
            </lane>
            <label text={StatusText} />
            <lane *if={ShowSelectionConfirmation} layout="stretch content" orientation="vertical">
                <label text="Unsaved changes detected. Confirm before switching animals." />
                <lane>
                    <button text="Switch anyway" click=|ConfirmSelectionChange()| />
                    <button text="Stay here" click=|CancelSelectionChange()| />
                </lane>
            </lane>
            <lane *if={ShowCloseConfirmation} layout="stretch content" orientation="vertical">
                <label text="Unsaved changes detected. Close the menu again to leave, or cancel here." />
                <lane>
                    <button text="Arm close" click=|ConfirmClose()| />
                    <button text="Keep editing" click=|CancelClose()| />
                </lane>
            </lane>
        </lane>
        <lane layout="stretch stretch">
            <scrollable layout="400px stretch" peeking="48">
                <lane orientation="vertical">
                    <label *if={HasHiddenSelection} text="Selected animal is hidden by the current filters." />
                    <label *if={ShowEmptyState} text="No animals match the current search and filters." />
                    <expander *repeat={AnimalGroups} layout="stretch content" is-expanded="true">
                        <label *outlet="header" text={Title} />
                        <lane orientation="vertical" margin="16,8,0,8">
                            <lane *repeat={Animals} orientation="vertical">
                                <lane layout="stretch content">
                                    <image *if={HasPreviewSprite} layout="72px 72px" sprite={PreviewSprite} />
                                    <lane orientation="vertical">
                                        <button layout="stretch content"
                                                text={ButtonText}
                                                click=|OpenAnimal()| />
                                        <label text={MetaText} />
                                        <lane>
                                            <label *repeat={Badges} text={Text} />
                                        </lane>
                                    </lane>
                                </lane>
                            </lane>
                        </lane>
                    </expander>
                </lane>
            </scrollable>
            <scrollable layout="stretch stretch" peeking="64">
                <lane *if={HasSelectedAnimal} *context={SelectedAnimal} orientation="vertical">
                    <lane layout="stretch content">
                        <lane orientation="vertical">
                            <label font="dialogue" text={Key} />
                            <label text={HeaderStatus} />
                            <label text={DirtySummary} />
                            <label text={PreviewStatus} />
                            <label text={ScalarSummary} />
                        </lane>
                        <lane orientation="vertical">
                            <image *if={HasPreviewSprite} layout="192px 192px" sprite={PreviewSprite} />
                            <label *if={MissingPreviewSprite} text="No preview sprite available." />
                        </lane>
                    </lane>
                    <lane>
                        <button text={OverviewTabText} click=|ShowOverviewTab()| />
                        <button text={LocationTypesTabText} click=|ShowLocationTypesTab()| />
                        <button text={ModKeysTabText} click=|ShowModKeysTab()| />
                        <button text={WarningsTabText} click=|ShowWarningsTab()| />
                    </lane>

                    <lane *if={IsOverviewTab} orientation="vertical">
                        <label text="Overview" />
                        <label text={PreviewStatus} />
                        <label text={ScalarSummary} />
                        <label text={DirtySummary} />
                    </lane>

                    <lane *if={IsLocationTypesTab} orientation="vertical">
                        <label text={LocationTypesSectionLabel} />
                        <lane *repeat={LocationTypes} orientation="vertical">
                            <checkbox label-text={Label} is-checked={<>Allowed} />
                        </lane>
                    </lane>

                    <lane *if={IsModKeysTab} orientation="vertical">
                        <label text={ModGroupsSectionLabel} />
                        <label text="Only mod-group editing is shown here." />
                        <lane *repeat={ModGroups} orientation="vertical">
                            <label text={ModId} />
                            <label text={BaseSummary} />
                            <label text={EffectiveLabel} />
                            <textinput layout="stretch 54px" text={<>KeysText} />
                            <label text={WarningText} />
                            <lane>
                                <button text="Reset to base" click=|ResetToBase()| />
                                <button text="Clear override" click=|ClearOverride()| />
                                <button text="Remove group" click=|RemoveGroup()| />
                            </lane>
                        </lane>
                        <lane orientation="vertical">
                            <label text="Add custom mod group" />
                            <lane>
                                <textinput layout="stretch 54px" text={<>NewModGroupId} />
                                <button text="Add" click=|AddModGroup()| />
                            </lane>
                        </lane>
                    </lane>

                    <lane *if={IsWarningsTab} orientation="vertical">
                        <label text="Warnings" />
                        <label text={WarningSummary} />
                    </lane>

                    <lane *if={IsOverviewTab} orientation="vertical">
                        <label text={ScalarSectionLabel} />
                        <lane>
                            <checkbox label-text="Enabled" is-checked={<>Enabled} />
                            <button text="Reset" click=|ResetEnabled()| />
                        </lane>
                        <label text={EnabledComparison} />
                        <lane>
                            <checkbox label-text="Large egg" is-checked={<>LargeEgg} />
                            <button text="Reset" click=|ResetLargeEgg()| />
                        </lane>
                        <label text={LargeEggComparison} />
                        <lane>
                            <checkbox label-text="Magic egg" is-checked={<>IsMagic} />
                            <button text="Reset" click=|ResetIsMagic()| />
                        </lane>
                        <label text={MagicComparison} />
                        <lane orientation="vertical">
                            <label text="Price" />
                            <lane>
                                <textinput layout="220px 54px" text={<>PriceText} />
                                <button text="Reset" click=|ResetPrice()| />
                            </lane>
                            <label text={PriceComparison} />
                            <label *if={HasValidationErrors} text={PriceError} />
                        </lane>
                    </lane>
                </lane>
            </scrollable>
        </lane>
    </lane>
</frame>
