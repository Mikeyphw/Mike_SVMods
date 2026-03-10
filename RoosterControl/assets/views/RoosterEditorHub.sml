<frame layout="1380px 860px"
       background={@Mods/StardewUI/Sprites/MenuBackground}
       border={@Mods/StardewUI/Sprites/MenuBorder}
       border-thickness="36, 36, 40, 36"
       padding="28,20,28,24">
    <lane layout="stretch stretch" orientation="vertical">
        <banner text={Title} margin="0,0,0,12" />

        <lane orientation="horizontal" margin="0,0,0,16">
            <frame background={@Mods/StardewUI/Sprites/ControlBorder} padding="16,12,16,10" margin="0,0,10,0">
                <button text={CompatTabText} click=|ShowCompatTab()| />
            </frame>
            <frame background={@Mods/StardewUI/Sprites/ControlBorder} padding="16,12,16,10">
                <button text={ProduceTabText} click=|ShowProduceTab()| />
            </frame>
            <frame background={@Mods/StardewUI/Sprites/ControlBorder} padding="16,12,16,10" margin="10,0,0,0">
                <button text={SettingsTabText} click=|ShowSettingsTab()| />
            </frame>
            <frame *if={HasDebugTab}
                   background={@Mods/StardewUI/Sprites/ControlBorder}
                   padding="16,12,16,10"
                   margin="10,0,0,0">
                <button text={DebugTabText} click=|ShowDebugTab()| />
            </frame>
        </lane>

        <frame layout="stretch stretch"
               background={@Mods/StardewUI/Sprites/ControlBorder}
               padding="18,16,18,18">
            <lane *if={IsCompatTab} *context={:Compat} layout="stretch stretch" orientation="vertical">
                <frame layout="stretch content"
                       background={@Mods/StardewUI/Sprites/ControlBorder}
                       padding="20,16">
                    <lane orientation="vertical">
                        <label font="dialogue" text="Compat Overview" />
                        <label text={:SummaryText} margin="0,8,0,0" />
                    </lane>
                </frame>

                <frame *if={HasValidation}
                       layout="stretch content"
                       background={@Mods/StardewUI/Sprites/ControlBorder}
                       margin="0,16,0,0"
                       padding="20,16">
                    <lane orientation="vertical">
                        <label font="dialogue" text="Validation" />
                        <label text={:ValidationText} margin="0,8,0,0" />
                    </lane>
                </frame>

                <lane layout="stretch stretch" orientation="horizontal" margin="0,16,0,0">
                    <scrollable layout="360px stretch" peeking="48">
                        <lane orientation="vertical">
                            <label text="Egg Sources" margin="0,0,0,8" />
                            <frame *repeat={:EggGroups}
                                   layout="stretch content"
                                   background={@Mods/StardewUI/Sprites/ControlBorder}
                                   margin="0,0,0,12"
                                   padding="16,14">
                                <lane orientation="vertical">
                                    <button layout="stretch content" text={:ButtonText} tooltip={:MetaText} click=|SelectEggGroup()| />
                                </lane>
                            </frame>
                        </lane>
                    </scrollable>

                    <scrollable layout="stretch stretch" peeking="48">
                        <lane orientation="vertical">
                            <frame layout="stretch content"
                                   background={@Mods/StardewUI/Sprites/ControlBorder}
                                   padding="20,16">
                                <lane orientation="vertical">
                                    <label font="dialogue" text={SelectedEggGroupName} />
                                    <label text="Egg group configuration" margin="0,8,0,0" />
                                </lane>
                            </frame>

                            <frame *if={HasSelectedEggGroup}
                                   *context={:SelectedEggGroup}
                                   layout="stretch content"
                                   background={@Mods/StardewUI/Sprites/ControlBorder}
                                   margin="0,16,0,0"
                                   padding="20,16">
                                <lane orientation="vertical">
                                    <frame *repeat={:Kinds}
                                           layout="stretch content"
                                           background={@Mods/StardewUI/Sprites/ControlBorder}
                                           margin="0,0,0,12"
                                           padding="16,14">
                                        <lane orientation="vertical">
                                            <label font="dialogue" text={:DisplayName} />
                                            <lane layout="stretch content" margin="0,8,0,8">
                                                <dropdown layout="stretch content"
                                                          options={:AvailableSources}
                                                          selected-option={<>SelectedSourceId} />
                                            </lane>
                                            <label text={:ValidationNote} />
                                        </lane>
                                    </frame>
                                </lane>
                            </frame>

                            <frame layout="stretch content"
                                   background={@Mods/StardewUI/Sprites/ControlBorder}
                                   margin="0,16,0,0"
                                   padding="20,16">
                                <lane orientation="vertical">
                                    <label font="dialogue" text="Providers" />
                                    <button layout="240px content" text="Use all providers" click=|UseAllProviders()| margin="0,8,0,12" />
                                    <frame *repeat={:ProviderOptions}
                                           layout="stretch content"
                                           background={@Mods/StardewUI/Sprites/ControlBorder}
                                           margin="0,0,0,10"
                                           padding="16,14">
                                        <lane orientation="vertical">
                                            <button layout="stretch content" text={:ButtonText} tooltip={:DescriptionText} click=|SelectProvider()| />
                                        </lane>
                                    </frame>
                                    <label text="Selected provider" margin="0,8,0,0" />
                                    <label text={SelectedProviderName} />
                                </lane>
                            </frame>
                        </lane>
                    </scrollable>
                </lane>

                <lane layout="stretch content" margin="0,12,0,0">
                    <button text="Save" click=|Save()| />
                    <button text="Discard" click=|Discard()| />
                    <button text="Reload" click=|Reload()| />
                    <button text="Reset" click=|ResetSelection()| />
                </lane>
            </lane>

            <lane *if={IsProduceTab} *context={:Produce} layout="stretch stretch" orientation="vertical">
                <lane layout="stretch content" orientation="horizontal" margin="0,0,0,12">
                    <frame *repeat={:Roosters}
                           background={@Mods/StardewUI/Sprites/ControlBorder}
                           padding="12,10,12,10"
                           margin="0,0,10,0">
                        <button text={:ButtonText} tooltip={:MetaText} click=|OpenRooster()| />
                    </frame>
                </lane>

                <scrollable layout="stretch stretch" peeking="64">
                    <lane orientation="vertical">
                        <frame layout="stretch content"
                               background={@Mods/StardewUI/Sprites/ControlBorder}
                               padding="20,16">
                            <lane layout="stretch content" orientation="horizontal">
                                <lane layout="stretch content" orientation="vertical">
                                    <label font="dialogue" text={SelectedRoosterName} />
                                    <label text={SummaryText} margin="0,8,0,0" />
                                    <label text={ModeSummaryText} />
                                </lane>
                                <lane layout="200px content" horizontal-content-alignment="end">
                                    <image *if={HasSelectedRoosterSprite}
                                           layout="120px 120px"
                                           sprite={SelectedRoosterSprite} />
                                </lane>
                            </lane>
                        </frame>

                        <frame layout="stretch content"
                               background={@Mods/StardewUI/Sprites/ControlBorder}
                               margin="0,16,0,0"
                               padding="20,16">
                            <lane orientation="vertical">
                                <label text="Modes" />
                                <lane layout="stretch content" margin="0,10,0,0">
                                    <label layout="220px content" text="Normal mode" />
                                    <dropdown layout="420px content"
                                              options={:ProduceModeOptions}
                                              selected-option={<>SelectedProduceMode} />
                                </lane>
                                <lane layout="stretch content" margin="0,8,0,0">
                                    <label layout="220px content" text="Deluxe mode" />
                                    <dropdown layout="420px content"
                                              options={:DeluxeModeOptions}
                                              selected-option={<>SelectedDeluxeMode} />
                                </lane>
                                <label text={ActiveSectionSummary} margin="0,12,0,0" />
                            </lane>
                        </frame>

                        <lane orientation="horizontal" margin="0,16,0,12">
                            <frame background={@Mods/StardewUI/Sprites/ControlBorder} padding="12,10,12,10" margin="0,0,10,0">
                                <button text={NormalTabText} click=|ShowNormalSection()| />
                            </frame>
                            <frame background={@Mods/StardewUI/Sprites/ControlBorder} padding="12,10,12,10">
                                <button text={DeluxeTabText} click=|ShowDeluxeSection()| />
                            </frame>
                        </lane>

                        <frame *repeat={:CurrentSlots}
                               layout="stretch content"
                               background={@Mods/StardewUI/Sprites/ControlBorder}
                               margin="0,0,0,12"
                               padding="18,16">
                            <lane orientation="vertical">
                                <lane layout="stretch content" orientation="horizontal">
                                    <lane layout="stretch content" orientation="vertical">
                                        <label font="dialogue" text={:SlotTitle} />
                                        <label text={:DisplayLabel} tooltip={:StableKey} margin="0,8,0,0" />
                                        <label text={:Category} />
                                    </lane>
                                    <lane layout="96px content" horizontal-content-alignment="end">
                                        <image *if={:HasPreviewSprite}
                                               layout="72px 72px"
                                               sprite={:PreviewSprite}
                                               tooltip={:StableKey} />
                                    </lane>
                                </lane>

                                <label text="Choice" margin="0,12,0,0" />
                                <dropdown layout="stretch content"
                                          options={:Choices}
                                          selected-option={<>SelectedChoice} />

                                <lane *if={IsCustom} orientation="vertical" margin="0,10,0,0">
                                    <label text="Custom item" />
                                    <textinput layout="stretch content" text={<>CustomItemName} />
                                    <lane layout="stretch content" margin="0,8,0,0">
                                        <button text="Validate" click=|ValidateCustomItem()| />
                                        <button text="Pick existing item" click=|ShowExistingItemPicker()| />
                                    </lane>
                                    <label text={:ValidationText} margin="0,8,0,0" />
                                    <frame *if={IsPickingExistingItem}
                                           layout="stretch content"
                                           background={@Mods/StardewUI/Sprites/ControlBorder}
                                           margin="0,12,0,0"
                                           padding="16,14">
                                        <lane orientation="vertical">
                                            <lane layout="stretch content" orientation="horizontal">
                                                <label layout="stretch content" text="Choose existing item" />
                                                <button text="Close" click=|HideExistingItemPicker()| />
                                            </lane>
                                            <textinput layout="stretch content" margin="0,10,0,10" text={<>ExistingItemSearchText} />
                                            <scrollable layout="stretch 280px" peeking="48">
                                                <grid layout="stretch content" item-layout="length: 72">
                                                    <frame *repeat={:VisibleExistingItems}
                                                           background={@Mods/StardewUI/Sprites/ControlBorder}
                                                           padding="8"
                                                           margin="0,0,10,10">
                                                        <lane layout="stretch content" horizontal-content-alignment="middle" vertical-content-alignment="middle">
                                                            <image *if={:HasPreviewSprite}
                                                                   layout="48px 48px"
                                                                   sprite={:PreviewSprite}
                                                                   focusable="true"
                                                                   tooltip={:TooltipText}
                                                                   click=|Pick()| />
                                                            <button *if={:MissingPreviewSprite}
                                                                    layout="48px 48px"
                                                                    text="?"
                                                                    tooltip={:TooltipText}
                                                                    click=|Pick()| />
                                                        </lane>
                                                    </frame>
                                                </grid>
                                            </scrollable>
                                        </lane>
                                    </frame>
                                </lane>

                                <lane layout="stretch content" margin="0,10,0,0">
                                    <label layout="72px content" text="Weight" />
                                    <slider track-width="180"
                                            min="0"
                                            max="100"
                                            interval="1"
                                            value={<>Weight} />
                                    <label text={:WeightText} margin="12,0,0,0" />
                                </lane>
                            </lane>
                        </frame>

                        <lane layout="stretch content" margin="0,12,0,0">
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
                            <label text="Compat editor keybind" margin="0,12,0,0" />
                            <label text={CompatKeybindText} />
                            <label text={CompatAvailabilityText} />
                            <label text="Produce editor keybind" margin="0,12,0,0" />
                            <label text={ProduceKeybindText} />
                            <label text={ProduceAvailabilityText} />
                        </lane>
                    </frame>

                    <frame layout="stretch content"
                           background={@Mods/StardewUI/Sprites/ControlBorder}
                           margin="0,16,0,0"
                           padding="20,16">
                        <lane orientation="vertical">
                            <label font="dialogue" text="General" />
                            <label text="Update rate" margin="0,10,0,0" />
                            <dropdown layout="320px content"
                                      options={:UpdateRateOptions}
                                      selected-option={<>SelectedUpdateRate} />
                            <label text="Harvest type" margin="0,10,0,0" />
                            <dropdown layout="320px content"
                                      options={:HarvestTypeOptions}
                                      selected-option={<>SelectedHarvestType} />
                        </lane>
                    </frame>

                    <frame layout="stretch content"
                           background={@Mods/StardewUI/Sprites/ControlBorder}
                           margin="0,16,0,0"
                           padding="20,16">
                        <lane orientation="vertical">
                            <label font="dialogue" text="Probabilities" />
                            <checkbox label-text="Enable egg probability" is-checked={<>EnableEggProbability} />
                            <label text="Egg probability" margin="0,8,0,0" />
                            <textinput layout="180px 54px" text={<>EggProbabilityText} />
                            <checkbox label-text="Enable large egg probability" is-checked={<>EnableLargeEggProbability} margin="0,10,0,0" />
                            <label text="Large egg probability" margin="0,8,0,0" />
                            <textinput layout="180px 54px" text={<>LargeEggProbabilityText} />
                            <label text="Fertilized egg probability" margin="0,10,0,0" />
                            <textinput layout="180px 54px" text={<>FertilizedEggProbabilityText} />
                            <label text="Rooster to chicken ratio" margin="0,10,0,0" />
                            <textinput layout="180px 54px" text={<>RoosterToChickenRatioText} />
                            <label text="Boost by rooster" margin="0,10,0,0" />
                            <textinput layout="180px 54px" text={<>BoostByRoosterText} />
                            <label text="Chosen daily luck bonus" margin="0,10,0,0" />
                            <textinput layout="180px 54px" text={<>ChosenDailyLuckBonusText} />
                        </lane>
                    </frame>

                    <frame layout="stretch content"
                           background={@Mods/StardewUI/Sprites/ControlBorder}
                           margin="0,16,0,0"
                           padding="20,16">
                        <lane orientation="vertical">
                            <label font="dialogue" text="Rooster Settings" />
                            <label text="Rooster type" margin="0,10,0,0" />
                            <dropdown layout="320px content"
                                      options={:RoosterTypeOptions}
                                      selected-option={<>SelectedRoosterTypeName} />
                            <label text="Required building" margin="0,10,0,0" />
                            <dropdown layout="320px content"
                                      options={:RequiredBuildingOptions}
                                      selected-option={<>SelectedRequiredBuilding} />
                            <checkbox label-text="Buyable" is-checked={<>RoosterBuyable} margin="0,10,0,0" />
                            <checkbox label-text="Enable rooster hatching" is-checked={<>EnableRoosterHatching} />
                            <checkbox label-text="Edit all females" is-checked={<>EditAllFemales} />
                            <label text="Price" margin="0,10,0,0" />
                            <textinput layout="180px 54px" text={<>PriceText} />
                            <label text="Mature days" margin="0,10,0,0" />
                            <textinput layout="180px 54px" text={<>MatureDaysText} />
                            <label text="Produce days" margin="0,10,0,0" />
                            <textinput layout="180px 54px" text={<>ProduceDaysText} />
                            <lane *if={IsBlueRooster} orientation="vertical" margin="0,10,0,0">
                                <checkbox label-text="Bypass blue conditions" is-checked={<>BypassBlueConditions} />
                                <label text="Blue egg choice" margin="0,8,0,0" />
                                <textinput layout="320px 54px" text={<>BlueEggChoiceText} />
                            </lane>
                        </lane>
                    </frame>

                    <frame layout="stretch content"
                           background={@Mods/StardewUI/Sprites/ControlBorder}
                           margin="0,16,0,0"
                           padding="20,16">
                        <lane orientation="vertical">
                            <label font="dialogue" text="Validation" />
                            <label text={ValidationText} margin="0,8,0,0" />
                        </lane>
                    </frame>

                    <lane layout="stretch content" margin="0,16,0,0">
                        <button text="Save" click=|Save()| />
                        <button text="Discard" click=|Discard()| />
                        <button text="Reload" click=|Reload()| />
                    </lane>
                </lane>
            </scrollable>

            <scrollable *if={IsDebugTab} *context={:Debug} layout="stretch stretch" peeking="64">
                <lane orientation="vertical">
                    <frame layout="stretch content"
                           background={@Mods/StardewUI/Sprites/ControlBorder}
                           padding="20,16">
                        <lane orientation="vertical">
                            <label font="dialogue" text="Flags" />
                            <checkbox label-text="Enable debug" is-checked={<>EnableDebug} />
                            <checkbox label-text="Verbose logging" is-checked={<>VerboseLogging} margin="0,10,0,0" />
                            <checkbox label-text="Testing logging" is-checked={<>TestingLogging} />
                        </lane>
                    </frame>

                    <frame layout="stretch content"
                           background={@Mods/StardewUI/Sprites/ControlBorder}
                           margin="0,16,0,0"
                           padding="20,16">
                        <lane orientation="vertical">
                            <label font="dialogue" text="Luck Tuning" />
                            <label text="Bonus calculation multiplier" margin="0,8,0,0" />
                            <textinput layout="220px 54px" text={<>BonusCalculationMultiplierText} />
                            <label text="Super unlucky bonus multiplier" margin="0,10,0,0" />
                            <textinput layout="220px 54px" text={<>SuperUnluckyBonusMultiplierText} />
                            <label text="Super lucky bonus multiplier" margin="0,10,0,0" />
                            <textinput layout="220px 54px" text={<>SuperLuckyBonusMultiplierText} />
                            <label text="Luck adjustment offset" margin="0,10,0,0" />
                            <textinput layout="220px 54px" text={<>LuckAdjustmentOffsetText} />
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
        </frame>
    </lane>
</frame>
