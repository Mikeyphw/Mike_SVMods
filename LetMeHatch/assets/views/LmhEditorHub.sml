<frame layout="1380px 860px"
       background={@Mods/StardewUI/Sprites/MenuBackground}
       border={@Mods/StardewUI/Sprites/MenuBorder}
       border-thickness="36, 36, 40, 36"
       padding="28,20,28,24">
    <lane layout="stretch stretch" orientation="vertical">
        <banner text={Title} margin="0,0,0,12" />

        <lane orientation="horizontal" margin="0,0,0,16">
            <frame background={@Mods/StardewUI/Sprites/ControlBorder} padding="16,12,16,10" margin="0,0,10,0">
                <button text={BreedingTabText} click=|ShowBreedingTab()| />
            </frame>
            <frame background={@Mods/StardewUI/Sprites/ControlBorder} padding="16,12,16,10" margin="0,0,10,0">
                <button text={ColorsTabText} click=|ShowColorsTab()| />
            </frame>
            <frame background={@Mods/StardewUI/Sprites/ControlBorder} padding="16,12,16,10">
                <button text={MappingsTabText} click=|ShowMappingsTab()| />
            </frame>
            <frame background={@Mods/StardewUI/Sprites/ControlBorder} padding="16,12,16,10" margin="10,0,0,0">
                <button text={SettingsTabText} click=|ShowSettingsTab()| />
            </frame>
            <frame background={@Mods/StardewUI/Sprites/ControlBorder} padding="16,12,16,10" margin="10,0,0,0">
                <button text={DebugTabText} click=|ShowDebugTab()| />
            </frame>
        </lane>

        <frame layout="stretch stretch"
               background={@Mods/StardewUI/Sprites/ControlBorder}
               padding="18,16,18,18">
            <lane *if={IsBreedingTab} *context={:Breeding} layout="stretch stretch" orientation="vertical">
                <label text={SummaryText} margin="0,0,0,8" />
                <lane layout="stretch content" margin="0,0,0,12">
                    <label layout="220px content" text="Pack" />
                    <dropdown layout="420px content"
                              options={:PackOptions}
                              selected-option={<>SelectedPackOption} />
                </lane>
                <lane layout="stretch stretch" orientation="horizontal">
                    <scrollable layout="360px stretch" peeking="48">
                        <lane orientation="vertical">
                            <label text="Females" />
                            <label text={SelectedPack} margin="0,0,0,8" />
                            <grid layout="stretch content" item-layout="length: 92">
                                <lane *repeat={:Females}
                                      layout="stretch content"
                                      horizontal-content-alignment="middle"
                                      margin="0,0,14,14">
                                    <image *if={:HasPreviewSprite}
                                           layout="72px 72px"
                                           sprite={:PreviewSprite}
                                           focusable="true"
                                           tooltip={:TooltipText}
                                           click=|OpenFemale()| />
                                    <button *if={:MissingPreviewSprite}
                                            layout="72px 72px"
                                            text=""
                                            tooltip={:TooltipText}
                                            click=|OpenFemale()| />
                                </lane>
                            </grid>
                        </lane>
                    </scrollable>

                    <scrollable layout="stretch stretch" peeking="48">
                        <lane *if={HasSelectedFemale} orientation="vertical">
                            <frame layout="stretch content"
                                   background={@Mods/StardewUI/Sprites/ControlBorder}
                                   padding="20,16">
                                <lane orientation="vertical">
                                    <label font="dialogue" text={SelectedFemaleSummaryText} />
                                    <label text={ValidationText} margin="0,8,0,0" />
                                </lane>
                            </frame>

                            <frame layout="stretch content"
                                   background={@Mods/StardewUI/Sprites/ControlBorder}
                                   margin="0,20,0,0"
                                   padding="20,16">
                                <lane orientation="vertical">
                                    <label text="Female rules" />
                                    <label text={ApplyDailyLuckBoostLabel} margin="0,8,0,0" />
                                    <button layout="220px content" text="Toggle daily boost" click=|ToggleApplyDailyLuckBoost()| />
                                    <label text={EggChanceSummaryText} margin="0,16,0,0" />
                                    <label text="Normal egg chance" margin="0,8,0,0" />
                                    <textinput layout="180px 54px" text={<>NormalEggChanceText} />
                                    <label text="Large egg chance" margin="0,8,0,0" />
                                    <textinput layout="180px 54px" text={<>LargeEggChanceText} />
                                    <label text="Fertilized egg chance" margin="0,8,0,0" />
                                    <textinput layout="180px 54px" text={<>FertilizedEggChanceText} />
                                </lane>
                            </frame>

                            <frame layout="stretch content"
                                   background={@Mods/StardewUI/Sprites/ControlBorder}
                                   margin="0,20,0,0"
                                   padding="20,16">
                                <lane orientation="vertical">
                                    <label text="Male rules" />
                                    <label text={SelectedMaleSummaryText} margin="0,8,0,0" />
                                    <label text={SelectedMaleEggsText} />
                                    <label text={SelectedMaleEnabledLabel} margin="0,8,0,0" />
                                    <button layout="220px content" text="Toggle male enabled" click=|ToggleSelectedMaleEnabled()| />
                                    <label text={SelectedMaleAlternatePurchaseLabel} margin="0,8,0,0" />
                                    <button layout="220px content" text="Toggle alternate purchase" click=|ToggleSelectedMaleAlternatePurchase()| />
                                    <label text="Male chance" margin="0,8,0,0" />
                                    <textinput layout="180px 54px" text={<>SelectedMaleChanceText} />
                                    <label text="Boost by male" margin="0,8,0,0" />
                                    <textinput layout="180px 54px" text={<>SelectedMaleBoostText} />
                                </lane>
                            </frame>
                        </lane>
                    </scrollable>
                </lane>
                <lane layout="stretch content" margin="0,12,0,0">
                    <button text="Save" click=|Save()| />
                    <button text="Discard" click=|Discard()| />
                    <button text="Reload" click=|Reload()| />
                </lane>
            </lane>

            <lane *if={IsColorsTab} *context={:Colors} layout="stretch stretch" orientation="vertical">
                <label text={SelectedEggSummaryText} />
                <label text={PreviewStatusText} margin="0,0,0,8" />
                <lane layout="stretch content" orientation="horizontal" margin="0,0,0,12">
                    <frame background={@Mods/StardewUI/Sprites/ControlBorder} padding="12,10,12,10" margin="0,0,10,0">
                        <button text={SourceEggsTabText} click=|ShowSourceEggs()| />
                    </frame>
                    <frame background={@Mods/StardewUI/Sprites/ControlBorder} padding="12,10,12,10">
                        <button text={ClonedEggsTabText} click=|ShowClonedEggs()| />
                    </frame>
                </lane>
                <lane layout="stretch content" margin="0,0,0,12">
                    <button text="Save" click=|Save()| />
                    <button text="Discard" click=|Discard()| />
                    <button text="Reload" click=|Reload()| />
                </lane>

                <scrollable *if={IsBrowsing} layout="stretch stretch" peeking="48">
                    <grid layout="stretch content" item-layout="length: 92">
                        <lane *repeat={OverrideRows}
                              layout="stretch content"
                              horizontal-content-alignment="middle"
                              margin="0,0,14,14">
                            <image *if={:HasPreviewSprite}
                                   layout="72px 72px"
                                   sprite={:PreviewSprite}
                                   focusable="true"
                                   tooltip={:TooltipText}
                                   click=|SelectOverrideRow()| />
                            <button *if={:MissingPreviewSprite}
                                    layout="72px 72px"
                                    text=""
                                    tooltip={:TooltipText}
                                    click=|SelectOverrideRow()| />
                        </lane>
                    </grid>
                </scrollable>

                <scrollable *if={IsShowingDetail} layout="stretch stretch" peeking="48">
                    <lane orientation="vertical">
                        <lane layout="stretch content" orientation="horizontal">
                            <button layout="220px content" text="Back to eggs" click=|BackToBrowser()| />
                            <lane layout="stretch content" orientation="horizontal" horizontal-content-alignment="end">
                                <button text="Previous" click=|OpenPreviousEgg()| />
                                <button text="Next" click=|OpenNextEgg()| />
                            </lane>
                        </lane>

                        <frame layout="stretch content"
                               background={@Mods/StardewUI/Sprites/ControlBorder}
                               margin="0,16,0,0"
                               padding="20,16">
                            <lane orientation="vertical" horizontal-content-alignment="middle">
                                <label font="dialogue" text={PreviewLabel} />
                                <image *if={HasPreviewSprite} layout="96px 96px" sprite={PreviewSprite} margin="0,12,0,12" />
                                <label *if={MissingPreviewSprite} text="No egg preview available." margin="0,8,0,8" />
                                <label text={SelectedEggSummaryText} />
                                <label text={SelectedColorPreviewText} />
                            </lane>
                        </frame>

                        <frame layout="stretch content"
                               background={@Mods/StardewUI/Sprites/ControlBorder}
                               margin="0,20,0,0"
                               padding="20,16">
                            <lane *if={IsSourceEggMode} orientation="vertical">
                                <label text="Palette Groups" />
                                <label text={SelectedPaletteGroupSummaryText} margin="0,8,0,0" />
                                <label text={EnabledPaletteGroupsText} tooltip={PreviewStatusText} margin="0,0,0,8" />
                                <grid layout="stretch content" item-layout="length: 92">
                                    <lane *repeat={PaletteGroups}
                                          layout="stretch content"
                                          horizontal-content-alignment="middle"
                                          margin="0,0,14,14">
                                        <image *if={:HasSwatchSprite}
                                               layout="56px 56px"
                                               sprite={:SwatchSprite}
                                               focusable="true"
                                               tooltip={:MetaText}
                                               click=|OpenPaletteGroup()| />
                                        <button *if={:MissingSwatchSprite}
                                                layout="56px 56px"
                                                text=""
                                                tooltip={:MetaText}
                                                click=|OpenPaletteGroup()| />
                                    </lane>
                                </grid>
                                <button layout="260px content" text="Toggle selected group" click=|ToggleSelectedPaletteGroup()| />
                            </lane>
                            <lane *if={IsClonedEggMode} orientation="vertical">
                                <label text="Palette policy comes from the source egg." />
                                <label text="Cloned eggs only expose explicit final color override here." margin="0,8,0,0" />
                            </lane>
                        </frame>

                        <frame layout="stretch content"
                               background={@Mods/StardewUI/Sprites/ControlBorder}
                               margin="0,20,0,0"
                               padding="20,16">
                            <lane orientation="vertical">
                                <label text="Override" />
                                <label *if={IsSourceEggMode} text="Source eggs only define palette groups for automatic clone coloring." margin="0,8,0,0" />
                                <label *if={IsClonedEggMode} text="Use the picker or hex field to force a final cloned-egg color." margin="0,8,0,0" />
                                <textinput layout="220px 54px" text={<>SelectedOverrideColorText} />
                                <button layout="180px content" text="Reset override" click=|ResetOverride()| margin="0,8,0,0" />
                            </lane>
                        </frame>

                        <lane layout="stretch content" margin="0,20,0,0">
                            <button text="Save" click=|Save()| />
                            <button text="Discard" click=|Discard()| />
                            <button text="Reload" click=|Reload()| />
                        </lane>
                    </lane>
                </scrollable>
            </lane>

            <lane *if={IsMappingsTab} *context={:Mappings} layout="stretch stretch" orientation="vertical">
                <label text={SummaryText} margin="0,0,0,8" />
                <lane layout="stretch content" margin="0,0,0,12">
                    <button text="Save" click=|Save()| />
                    <button text="Discard" click=|Discard()| />
                    <button text="Reload" click=|Reload()| />
                </lane>

                <lane layout="stretch content" orientation="horizontal" margin="0,0,0,12">
                    <frame *repeat={:MappingGroups}
                           background={@Mods/StardewUI/Sprites/ControlBorder}
                           padding="12,10,12,10"
                           margin="0,0,10,0">
                        <button text={:ButtonText} tooltip={:MetaText} click=|OpenGroup()| />
                    </frame>
                </lane>

                <scrollable *if={IsBrowsing} layout="stretch stretch" peeking="48">
                    <grid layout="stretch content" item-layout="length: 92">
                        <frame *repeat={:CurrentMappings}
                               background={@Mods/StardewUI/Sprites/ControlBorder}
                               padding="8,8,8,8"
                               margin="0,0,14,14">
                            <lane layout="stretch content"
                                  orientation="vertical"
                                  horizontal-content-alignment="middle">
                                <image *if={:HasAnimalPreviewSprite}
                                       layout="72px 72px"
                                       sprite={:AnimalPreviewSprite}
                                       focusable="true"
                                       tooltip={:TooltipText}
                                       click=|OpenMapping()| />
                                <image *if={:HasFallbackPreviewSprite}
                                       layout="72px 72px"
                                       sprite={:PreviewSprite}
                                       focusable="true"
                                       tooltip={:TooltipText}
                                       click=|OpenMapping()| />
                                <button *if={:MissingAllPreviewSprites}
                                        layout="72px 72px"
                                        text=""
                                        tooltip={:TooltipText}
                                        click=|OpenMapping()| />
                                <lane layout="stretch content"
                                      orientation="horizontal"
                                      horizontal-content-alignment="middle"
                                      margin="0,8,0,0">
                                    <image *if={:HasNormalProducePreviewSprite}
                                           layout="36px 36px"
                                           sprite={:NormalProducePreviewSprite}
                                           tooltip={:TooltipText} />
                                    <image *if={:HasLargeProducePreviewSprite}
                                           layout="36px 36px"
                                           sprite={:LargeProducePreviewSprite}
                                           margin="8,0,0,0"
                                           tooltip={:TooltipText} />
                                </lane>
                            </lane>
                        </frame>
                    </grid>
                </scrollable>

                <scrollable *if={IsShowingDetail} layout="stretch stretch" peeking="48">
                    <lane orientation="vertical">
                        <lane layout="stretch content" orientation="horizontal">
                            <button layout="240px content" text="Back to mappings" click=|BackToBrowser()| />
                            <lane layout="stretch content" orientation="horizontal" horizontal-content-alignment="end">
                                <button text="Previous" click=|OpenPreviousMapping()| />
                                <button text="Next" click=|OpenNextMapping()| />
                            </lane>
                        </lane>

                        <frame layout="stretch content"
                               background={@Mods/StardewUI/Sprites/ControlBorder}
                               margin="0,16,0,0"
                               padding="20,16">
                            <lane orientation="vertical">
                                <label font="dialogue" text={SelectedMapping} />
                                <label text={SelectionHelpText} margin="0,8,0,0" />
                            </lane>
                        </frame>

                        <frame layout="stretch content"
                               background={@Mods/StardewUI/Sprites/ControlBorder}
                               margin="0,20,0,0"
                               padding="20,16">
                            <lane orientation="vertical">
                                <label text="Base values" />
                                <label text="Normal" margin="0,8,0,0" />
                                <label text={BaseNormalItemText} />
                                <label text="Large" margin="0,8,0,0" />
                                <label text={BaseLargeItemText} />
                            </lane>
                        </frame>

                        <frame layout="stretch content"
                               background={@Mods/StardewUI/Sprites/ControlBorder}
                               margin="0,20,0,0"
                               padding="20,16">
                            <lane orientation="vertical">
                                <label text="Effective values" />
                                <label text="Normal" margin="0,8,0,0" />
                                <label text={EffectiveNormalItemText} />
                                <label text="Large" margin="0,8,0,0" />
                                <label text={EffectiveLargeItemText} />
                            </lane>
                        </frame>

                        <frame layout="stretch content"
                               background={@Mods/StardewUI/Sprites/ControlBorder}
                               margin="0,20,0,0"
                               padding="20,16">
                            <lane orientation="vertical">
                                <label text="Configured overrides" />
                                <label text="Normal item id" margin="0,8,0,0" />
                                <textinput layout="320px 54px" text={<>NormalOverrideText} />
                                <label text="Large item id" margin="0,8,0,0" />
                                <textinput layout="320px 54px" text={<>LargeOverrideText} />
                                <button layout="200px content" text="Reset mapping" click=|ResetMapping()| margin="0,12,0,0" />
                            </lane>
                        </frame>

                        <lane layout="stretch content" margin="0,20,0,0">
                            <button text="Save" click=|Save()| />
                            <button text="Discard" click=|Discard()| />
                            <button text="Reload" click=|Reload()| />
                        </lane>
                    </lane>
                </scrollable>
            </lane>

            <scrollable *if={IsSettingsTab} *context={:Settings} layout="stretch stretch" peeking="64">
                <lane orientation="vertical">
                    <frame layout="stretch content"
                           background={@Mods/StardewUI/Sprites/ControlBorder}
                           padding="20,16">
                        <lane orientation="vertical">
                            <label font="dialogue" text="Overview" />
                            <label text={SummaryText} margin="0,8,0,0" />
                            <label text="Breeding editor keybind" margin="0,12,0,0" />
                            <label text={BreedingKeybindText} />
                            <label text={BreedingAvailabilityText} />
                            <label text="Colors editor keybind" margin="0,12,0,0" />
                            <label text={ColorsKeybindText} />
                            <label text={ColorsAvailabilityText} />
                            <label text="Mappings editor keybind" margin="0,12,0,0" />
                            <label text={MappingsKeybindText} />
                            <label text={MappingsAvailabilityText} />
                        </lane>
                    </frame>

                    <frame layout="stretch content"
                           background={@Mods/StardewUI/Sprites/ControlBorder}
                           margin="0,16,0,0"
                           padding="20,16">
                        <lane orientation="vertical">
                            <label font="dialogue" text="General" />
                            <checkbox label-text="Edit egg list by name" is-checked={<>EditEggListByName} />
                            <checkbox label-text="Edit birth text" is-checked={<>EditBirthText} margin="0,8,0,0" />
                            <checkbox label-text="Alternate birth text" is-checked={<>AlternateBirthText} margin="0,8,0,0" />
                        </lane>
                    </frame>

                    <frame layout="stretch content"
                           background={@Mods/StardewUI/Sprites/ControlBorder}
                           margin="0,16,0,0"
                           padding="20,16">
                        <lane orientation="vertical">
                            <label font="dialogue" text="Edits" />
                            <label text="Animal" margin="0,10,0,0" />
                            <dropdown layout="420px content"
                                      options={:AnimalOptionKeys}
                                      selected-option={<>SelectedAnimalKey} />
                            <label text={SelectedAnimalLabel} margin="0,8,0,10" />
                            <checkbox label-text="Enable egg list edits" is-checked={<>EnableEggListEdit} />
                            <checkbox label-text="Enable birth text edits" is-checked={<>EnableBirthTextEdit} margin="0,8,0,0" />
                            <checkbox label-text="Enable clone eggs edits" is-checked={<>EnableCloneEggsEdit} margin="0,8,0,0" />
                        </lane>
                    </frame>

                    <frame layout="stretch content"
                           background={@Mods/StardewUI/Sprites/ControlBorder}
                           margin="0,16,0,0"
                           padding="20,16">
                        <lane orientation="vertical">
                            <label font="dialogue" text="Cloning" />
                            <checkbox label-text="Clone vanilla eggs" is-checked={<>CloneVanillaEggs} />
                            <checkbox label-text="Dino egg as artifact" is-checked={<>DinoEggAsArtifact} margin="0,8,0,0" />
                            <checkbox label-text="Clone machine rules" is-checked={<>CloneMachineRules} margin="0,8,0,0" />
                            <checkbox label-text="Use dynamic clone textures" is-checked={<>UseDynamicCloneTextures} margin="0,8,0,0" />
                            <label text="Dynamic clone tint mode" margin="0,10,0,0" />
                            <dropdown layout="320px content"
                                      options={:DynamicCloneTintModeOptions}
                                      selected-option={<>SelectedDynamicCloneTintMode} />
                            <checkbox label-text="Only fertilized eggs" is-checked={<>OnlyFertilizedEggs} margin="0,10,0,0" />
                            <checkbox label-text="Color cloned milk" is-checked={<>ColorClonedMilk} margin="0,8,0,0" />
                            <checkbox label-text="Exclusive rabbit wool" is-checked={<>ExclusiveRabbitWool} margin="0,8,0,0" />
                        </lane>
                    </frame>

                    <frame layout="stretch content"
                           background={@Mods/StardewUI/Sprites/ControlBorder}
                           margin="0,16,0,0"
                           padding="20,16">
                        <lane orientation="vertical">
                            <label font="dialogue" text="Breeding Globals" />
                            <checkbox label-text="Breeding system enabled" is-checked={<>BreedingSystemEnabled} />
                            <checkbox label-text="Deterministic breeding RNG" is-checked={<>DeterministicBreedingRng} margin="0,8,0,0" />
                            <label text="Daily luck bonus" margin="0,10,0,0" />
                            <textinput layout="180px 54px" text={<>DailyLuckBonusText} />
                            <label text="Normal egg chance" margin="0,10,0,0" />
                            <textinput layout="180px 54px" text={<>NormalEggChanceText} />
                            <label text="Large egg chance" margin="0,10,0,0" />
                            <textinput layout="180px 54px" text={<>LargeEggChanceText} />
                            <label text="Fertilized egg chance" margin="0,10,0,0" />
                            <textinput layout="180px 54px" text={<>FertilizedEggChanceText} />
                        </lane>
                    </frame>

                    <frame layout="stretch content"
                           background={@Mods/StardewUI/Sprites/ControlBorder}
                           margin="0,16,0,0"
                           padding="20,16">
                        <label text={ValidationText} />
                    </frame>

                    <lane layout="stretch content" margin="0,16,0,0">
                        <button text="Save" click=|Save()| />
                        <button text="Discard" click=|Discard()| />
                        <button text="Reload" click=|Reload()| />
                    </lane>
                </lane>
            </scrollable>

            <lane *if={IsDebugTab} *context={:Debug} layout="stretch stretch" orientation="vertical">
                <frame layout="stretch content"
                       background={@Mods/StardewUI/Sprites/ControlBorder}
                       padding="20,16">
                    <lane orientation="vertical">
                        <label font="dialogue" text="Debug" />
                        <checkbox label-text="Show edits" is-checked={<>ShowEdits} />
                        <checkbox label-text="Show animal keys" is-checked={<>ShowAnimalKeys} margin="0,8,0,0" />
                        <checkbox label-text="Verbose logging" is-checked={<>VerboseLogging} margin="0,8,0,0" />
                        <checkbox label-text="Testing logging" is-checked={<>TestingLogging} margin="0,8,0,0" />
                    </lane>
                </frame>

                <lane layout="stretch content" margin="0,16,0,0">
                    <button text="Save" click=|Save()| />
                    <button text="Discard" click=|Discard()| />
                    <button text="Reload" click=|Reload()| />
                </lane>
            </lane>
        </frame>
    </lane>
</frame>
