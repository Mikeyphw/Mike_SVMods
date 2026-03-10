<frame layout="1280px 840px"
       background={@Mods/StardewUI/Sprites/MenuBackground}
       border={@Mods/StardewUI/Sprites/MenuBorder}
       border-thickness="36, 36, 40, 36"
       padding="28,20,28,24">
    <lane layout="stretch stretch" orientation="vertical">
        <banner text={Title} margin="0,0,0,12" />

        <frame layout="stretch stretch"
               background={@Mods/StardewUI/Sprites/ControlBorder}
               padding="20,16,20,18">
            <lane *context={:WildAnimals} layout="stretch stretch" orientation="vertical">
                <lane layout="stretch content" orientation="horizontal">
                    <lane layout="stretch content" orientation="vertical">
                        <label text={HeaderSummary} />
                        <label text={StatsSummary} />
                        <label text={VisibilitySummary} />
                    </lane>
                    <lane layout="420px content" orientation="vertical" margin="24,0,0,0">
                        <label text="Status" />
                        <label text={DirtyStatusText} />
                    </lane>
                </lane>

                <lane layout="stretch content" orientation="vertical" margin="0,12,0,0">
                    <label text="Search" />
                    <textinput layout="420px 54px" text={<>SearchText} />
                </lane>

                <lane layout="stretch content" orientation="horizontal" margin="0,12,0,0">
                    <lane layout="190px content" orientation="vertical">
                        <label text="Magic filter" />
                        <dropdown layout="stretch content"
                                  options={:MagicFilterOptions}
                                  selected-option={<>SelectedMagicFilter} />
                    </lane>
                    <lane layout="190px content" orientation="vertical" margin="20,0,0,0">
                        <label text="Size filter" />
                        <dropdown layout="stretch content"
                                  options={:SizeFilterOptions}
                                  selected-option={<>SelectedSizeFilter} />
                    </lane>
                    <lane layout="stretch content" orientation="horizontal" margin="24,24,0,0">
                        <checkbox label-text="Overridden" is-checked={<>FilterOverridden} />
                        <checkbox label-text="Disabled" is-checked={<>FilterDisabled} />
                        <checkbox label-text="Warnings" is-checked={<>FilterWarnings} />
                        <checkbox label-text="Show invalid groups" is-checked={<>ShowInvalidAnimals} />
                    </lane>
                </lane>

                <lane layout="stretch content" orientation="horizontal" margin="0,8,0,0">
                    <button text="Save" click=|Save()| />
                    <button text="Discard" click=|Discard()| />
                    <button text="Reload" click=|Reload()| />
                    <button text="Add Wild Animal" click=|StartAddWildAnimal()| />
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

                <lane *if={IsAuthoring} layout="stretch stretch" margin="0,16,0,0">
                    <scrollable layout="stretch stretch" peeking="48">
                        <lane layout="stretch content" orientation="vertical">
                            <lane layout="stretch content" orientation="horizontal">
                                <button layout="220px content" text="Back to animals" click=|CancelAuthoring()| />
                                <lane layout="stretch content" orientation="horizontal" horizontal-content-alignment="end">
                                    <button text="Confirm" click=|ConfirmAuthoring()| />
                                </lane>
                            </lane>

                            <frame layout="stretch content"
                                   background={@Mods/StardewUI/Sprites/ControlBorder}
                                   margin="0,16,0,0"
                                   padding="20,16">
                                <lane orientation="vertical">
                                    <label font="dialogue" text={AuthoringTitle} />
                                    <label text={AuthoringHelpText} margin="0,8,0,0" />
                                    <label *if={IsAuthoringNewAnimal} text="Wild animal key" margin="0,12,0,0" />
                                    <textinput *if={IsAuthoringNewAnimal} layout="420px 54px" text={<>NewWildAnimalKey} />
                                    <label *if={IsAuthoringNewGroup} text="Mod group id" margin="0,12,0,0" />
                                    <textinput *if={IsAuthoringNewGroup} layout="420px 54px" text={<>AuthoringModGroupId} />
                                    <label text="Search farm animals" margin="0,12,0,0" />
                                    <textinput layout="420px 54px" text={<>PickerSearchText} />
                                    <label text={PickerSummary} margin="0,12,0,0" />
                                    <label *if={HasAuthoringError} text={AuthoringErrorText} margin="0,8,0,0" />
                                </lane>
                            </frame>

                            <grid layout="stretch content" item-layout="length: 150" margin="0,16,0,0">
                                <frame *repeat={:PickerItems}
                                       *if={:IsVisible}
                                       layout="stretch content"
                                       background={@Mods/StardewUI/Sprites/ControlBorder}
                                       margin="0,0,12,12"
                                       padding="12,10,12,12">
                                    <lane orientation="vertical" horizontal-content-alignment="middle">
                                        <image *if={:HasPreviewSprite} layout="56px 56px" sprite={:PreviewSprite} margin="0,0,0,8" />
                                        <button layout="stretch 40px" text="" tooltip={:TooltipText} click=|ToggleSelection()| />
                                        <label text={:SelectionText} margin="0,8,0,0" />
                                    </lane>
                                </frame>
                            </grid>

                            <lane layout="stretch content" orientation="horizontal" margin="0,20,0,0">
                                <button text="Confirm" click=|ConfirmAuthoring()| />
                                <button text="Cancel" click=|CancelAuthoring()| />
                            </lane>
                        </lane>
                    </scrollable>
                </lane>

                <lane *if={IsBrowsingWithoutAuthoring} layout="stretch stretch" margin="0,16,0,0">
                    <scrollable layout="stretch stretch" peeking="48">
                        <lane layout="stretch content" orientation="vertical">
                            <label *if={ShowEmptyState} text="No animals match the current search and filters." />
                            <label *if={HasHiddenSelection} text="Selected animal is hidden by the current filters." />
                            <grid layout="stretch content" item-layout="length: 108">
                                <lane *repeat={:VisibleAnimals}
                                      layout="stretch content"
                                      horizontal-content-alignment="middle"
                                      margin="0,0,18,18">
                                    <image *if={:HasPreviewSprite}
                                           layout="84px 84px"
                                           sprite={:PreviewSprite}
                                           focusable="true"
                                           tooltip={:DisplayTooltip}
                                           click=|OpenAnimal()| />
                                    <button *if={:MissingPreviewSprite}
                                            layout="84px 84px"
                                            text=""
                                            tooltip={:DisplayTooltip}
                                            click=|OpenAnimal()| />
                                </lane>
                            </grid>
                        </lane>
                    </scrollable>
                </lane>

                <lane *if={IsShowingDetail} layout="stretch stretch" margin="0,16,0,0">
                    <scrollable *context={SelectedAnimal} layout="stretch stretch" peeking="48">
                        <lane layout="stretch content" orientation="vertical">
                        <lane layout="stretch content" orientation="horizontal">
                            <button layout="220px content" text="Back to animals" click=|^BackToBrowser()| />
                            <lane layout="stretch content" orientation="horizontal" horizontal-content-alignment="end">
                                <button text="Previous" click=|^OpenPreviousAnimal()| />
                                <button text="Next" click=|^OpenNextAnimal()| />
                            </lane>
                        </lane>

                        <lane layout="stretch content" orientation="horizontal" margin="0,16,0,0">
                            <lane layout="200px content" orientation="vertical">
                                <label font="dialogue" text={Key} margin="0,0,0,8" />
                                <image *if={HasPreviewSprite} layout="144px 144px" sprite={PreviewSprite} />
                                <label *if={MissingPreviewSprite} text="No preview sprite available." />
                            </lane>
                            <frame layout="stretch content"
                                   margin="24,0,0,0"
                                   padding="20,16"
                                   background={@Mods/StardewUI/Sprites/ControlBorder}>
                                <lane orientation="vertical">
                                    <label text="Overview" />
                                    <label text="Status:" margin="0,8,0,0" />
                                    <label text={HeaderStatus} />
                                    <label text="Draft:" margin="0,8,0,0" />
                                    <label text={DirtySummary} />
                                    <label text="Preview:" margin="0,8,0,0" />
                                    <label text={PreviewStatus} />
                                    <label text="Enabled:" margin="0,8,0,0" />
                                    <label text={EnabledText} />
                                    <label text="Large Egg:" margin="0,8,0,0" />
                                    <label text={LargeEggText} />
                                    <label text="Magic:" margin="0,8,0,0" />
                                    <label text={MagicText} />
                                    <label text="Price:" margin="0,8,0,0" />
                                    <label text={PriceText} />
                                </lane>
                            </frame>
                        </lane>

                        <frame layout="stretch content" background={@Mods/StardewUI/Sprites/ControlBorder} margin="0,20,0,0" padding="20,16">
                            <lane orientation="vertical">
                                <label text={ProgressionSectionLabel} />
                                <label text={ProgressionSummaryText} margin="0,8,0,0" />
                                <lane *if={HasProgressionSettings} orientation="horizontal" margin="0,12,0,0">
                                    <button text="Use default" click=|UseDefaultProgression()| />
                                    <button text="Force enabled" click=|ForceProgressionEnabled()| />
                                    <button text="Force disabled" click=|ForceProgressionDisabled()| />
                                </lane>
                                <label *if={HasProgressionSettings} text="Default mode" margin="0,10,0,0" />
                                <label *if={HasProgressionSettings} text={ProgressionDefaultText} />
                                <label *if={HasProgressionSettings} text="Current mode" margin="0,8,0,0" />
                                <label *if={HasProgressionSettings} text={ProgressionCurrentText} />
                                <label *if={HasProgressionSettings} text="Effective mode" margin="0,8,0,0" />
                                <label *if={HasProgressionSettings} text={ProgressionEffectiveText} />
                            </lane>
                        </frame>

                        <frame layout="stretch content" background={@Mods/StardewUI/Sprites/ControlBorder} margin="0,20,0,0" padding="20,16">
                            <lane orientation="vertical">
                                <label text={ScalarSectionLabel} />
                                <lane orientation="horizontal" margin="0,10,0,0">
                                    <checkbox label-text="Enabled" is-checked={<>Enabled} />
                                    <button text="Reset" click=|ResetEnabled()| />
                                </lane>
                                <label text={EnabledComparison} />
                                <lane orientation="horizontal" margin="0,8,0,0">
                                    <checkbox label-text="Large egg" is-checked={<>LargeEgg} />
                                    <button text="Reset" click=|ResetLargeEgg()| />
                                </lane>
                                <label text={LargeEggComparison} />
                                <lane orientation="horizontal" margin="0,8,0,0">
                                    <checkbox label-text="Magic egg" is-checked={<>IsMagic} />
                                    <button text="Reset" click=|ResetIsMagic()| />
                                </lane>
                                <label text={MagicComparison} />
                                <label text="Price" margin="0,8,0,0" />
                                <lane orientation="horizontal">
                                    <textinput layout="220px 54px" text={<>PriceText} />
                                    <button text="Reset" click=|ResetPrice()| />
                                </lane>
                                <label text={PriceComparison} />
                                <label *if={HasValidationErrors} text={PriceError} />
                            </lane>
                        </frame>

                        <frame layout="stretch content" background={@Mods/StardewUI/Sprites/ControlBorder} margin="0,20,0,0" padding="20,16">
                            <lane orientation="vertical">
                                <label text={LocationTypesSectionLabel} />
                                <lane *repeat={:LocationTypes} orientation="vertical" margin="0,8,0,0">
                                    <checkbox label-text={Label} is-checked={<>Allowed} tooltip={ComparisonText} />
                                </lane>
                            </lane>
                        </frame>

                        <frame layout="stretch content" background={@Mods/StardewUI/Sprites/ControlBorder} margin="0,20,0,0" padding="20,16">
                            <lane orientation="vertical">
                                <label text={ModGroupsSectionLabel} />
                                <label text="Technical fields and base values are available in tooltips." />
                                <button text="Add Group" click=|^StartAddModGroup()| margin="0,12,0,0" />
                                <frame *repeat={:ModGroups}
                                       layout="stretch content"
                                       background={@Mods/StardewUI/Sprites/ControlBorder}
                                       margin="0,12,0,0"
                                       padding="16,14">
                                    <lane orientation="vertical">
                                        <label font="dialogue" text={:ModId} tooltip={BaseSummary} />
                                        <label text={EffectiveLabel} />
                                        <textinput layout="stretch 54px" text={<>KeysText} tooltip={WarningText} margin="0,8,0,8" />
                                        <lane orientation="horizontal">
                                            <button text="Reset to base" click=|ResetToBase()| />
                                            <button text="Clear override" click=|ClearOverride()| />
                                            <button text="Remove group" click=|RemoveGroup()| />
                                        </lane>
                                    </lane>
                                </frame>
                                <label text="Add custom mod group" margin="0,12,0,0" />
                                <lane orientation="horizontal">
                                    <textinput layout="stretch 54px" text={<>NewModGroupId} />
                                    <button text="Add" click=|AddModGroup()| />
                                </lane>
                            </lane>
                        </frame>

                        <frame layout="stretch content" background={@Mods/StardewUI/Sprites/ControlBorder} margin="0,20,0,0" padding="20,16">
                            <lane orientation="vertical">
                                <label text="Warnings" />
                                <label text={WarningSummary} />
                            </lane>
                        </frame>

                        <lane layout="stretch content" orientation="horizontal" margin="0,20,0,0">
                            <button text="Save" click=|^Save()| />
                            <button text="Discard" click=|^Discard()| />
                            <button text="Reload" click=|^Reload()| />
                        </lane>
                        </lane>
                    </scrollable>
                </lane>
            </lane>
        </frame>
    </lane>
</frame>
