<!--
Contract:
- MenuId: lmh.breeding.editor
- RootViewModel: LmhBreedingEditorViewModel
- Properties:
  - Title
  - Packs
  - SelectedPack
  - Females
  - SelectedFemale
  - HasSelectedFemale
  - SummaryText
  - Males
  - SelectedMale
  - HasSelectedMale
  - CanSave
  - CanDiscard
  - IsOverviewTab
  - IsRulesTab
  - ApplyDailyLuckBoostLabel
  - NormalEggChanceText
  - LargeEggChanceText
  - FertilizedEggChanceText
  - SelectedMaleEnabledLabel
  - SelectedMaleAlternatePurchaseLabel
  - SelectedMaleChanceText
  - SelectedMaleBoostText
  - SelectedMaleEggsText
- Methods:
  - OpenPack()
  - OpenFemale()
  - OpenMale()
  - ShowOverviewTab()
  - ShowRulesTab()
  - ToggleApplyDailyLuckBoost()
  - ToggleSelectedMaleEnabled()
  - ToggleSelectedMaleAlternatePurchase()
  - Save()
  - Discard()
  - Reload()
-->
<frame layout="1240px 780px"
       background={@Mods/StardewUI/Sprites/MenuBackground}
       border={@Mods/StardewUI/Sprites/MenuBorder}
       border-thickness="36, 36, 40, 36">
    <lane layout="stretch stretch" orientation="vertical" padding="24">
        <lane layout="stretch content" orientation="vertical">
            <banner text={Title} />
            <label text={SummaryText} />
        </lane>

        <lane layout="stretch stretch">
            <scrollable layout="300px stretch" peeking="48">
                <lane orientation="vertical">
                    <label text="Packs" />
                    <lane *repeat={Packs} orientation="vertical">
                        <button layout="stretch content"
                                text={ButtonText}
                                click=|OpenPack()| />
                        <label text={MetaText} />
                    </lane>
                </lane>
            </scrollable>

            <scrollable layout="320px stretch" peeking="48">
                <lane orientation="vertical">
                    <label text="Selected pack" />
                    <label text={SelectedPack} />
                    <label text="Females" />
                    <lane *repeat={Females} orientation="vertical">
                        <button layout="stretch content"
                                text={ButtonText}
                                click=|OpenFemale()| />
                        <label text={MetaText} />
                    </lane>
                </lane>
            </scrollable>

            <scrollable layout="stretch stretch" peeking="48">
                <lane *if={HasSelectedFemale} orientation="vertical">
                    <label text="Selected female" />
                    <label text={SelectedFemale} />
                    <lane>
                        <button text="Overview" click=|ShowOverviewTab()| />
                        <button text="Rules" click=|ShowRulesTab()| />
                    </lane>
                    <lane *if={IsOverviewTab} orientation="vertical">
                        <label text={ApplyDailyLuckBoostLabel} />
                        <button text="Toggle daily boost" click=|ToggleApplyDailyLuckBoost()| />
                        <label text="Males" />
                        <lane *repeat={Males} orientation="vertical">
                            <button layout="stretch content"
                                    text={ButtonText}
                                    click=|OpenMale()| />
                            <label text={MetaText} />
                        </lane>
                    </lane>
                    <lane *if={IsRulesTab} orientation="vertical">
                        <label text="Egg chances" />
                        <label text="Normal" />
                        <textinput layout="180px 54px" text={<>NormalEggChanceText} />
                        <label text="Large" />
                        <textinput layout="180px 54px" text={<>LargeEggChanceText} />
                        <label text="Fertilized" />
                        <textinput layout="180px 54px" text={<>FertilizedEggChanceText} />

                        <lane *if={HasSelectedMale} orientation="vertical">
                            <label text="Selected male" />
                            <label text={SelectedMale} />
                            <label text={SelectedMaleEggsText} />
                            <label text={SelectedMaleEnabledLabel} />
                            <button text="Toggle male enabled" click=|ToggleSelectedMaleEnabled()| />
                            <label text={SelectedMaleAlternatePurchaseLabel} />
                            <button text="Toggle alternate purchase" click=|ToggleSelectedMaleAlternatePurchase()| />
                            <label text="Male chance" />
                            <textinput layout="180px 54px" text={<>SelectedMaleChanceText} />
                            <label text="Boost by male" />
                            <textinput layout="180px 54px" text={<>SelectedMaleBoostText} />
                        </lane>
                    </lane>
                </lane>
            </scrollable>
        </lane>

        <lane layout="stretch content">
            <button text="Save" click=|Save()| />
            <button text="Discard" click=|Discard()| />
            <button text="Reload" click=|Reload()| />
        </lane>
    </lane>
</frame>
