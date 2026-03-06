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
  - CanSave
  - CanDiscard
  - IsOverviewTab
  - IsRulesTab
- Methods:
  - OpenPack()
  - OpenFemale()
  - ShowOverviewTab()
  - ShowRulesTab()
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
                        <label text="Overview tab placeholder" />
                        <label text="Future backing viewmodel supplies the final breeding summary." />
                    </lane>
                    <lane *if={IsRulesTab} orientation="vertical">
                        <label text="Rules tab placeholder" />
                        <label text="Future backing viewmodel supplies the final rule editor rows." />
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
