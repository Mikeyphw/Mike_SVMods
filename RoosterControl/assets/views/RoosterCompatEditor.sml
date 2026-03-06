<!--
Contract:
- MenuId: rooster.compat.editor
- RootViewModel: RoosterCompatEditorViewModel
- Properties:
  - Title
  - SummaryText
  - ValidationText
  - HasValidation
  - EggGroups
  - SelectedEggGroup
  - ProviderOptions
  - SelectedProvider
  - CanSave
  - CanDiscard
- Methods:
  - SelectEggGroup()
  - SelectProvider()
  - Save()
  - Discard()
  - Reload()
  - ResetSelection()
-->
<frame layout="1120px 760px"
       background={@Mods/StardewUI/Sprites/MenuBackground}
       border={@Mods/StardewUI/Sprites/MenuBorder}
       border-thickness="36, 36, 40, 36">
    <lane layout="stretch stretch" orientation="vertical" padding="24">
        <lane layout="stretch content" orientation="vertical">
            <banner text={Title} />
            <label text={SummaryText} />
            <label text="Save readiness is supplied by CanSave and CanDiscard in the future backing viewmodel." />
            <lane *if={HasValidation} orientation="vertical">
                <label text="Validation" />
                <label text={ValidationText} />
            </lane>
        </lane>

        <lane layout="stretch stretch">
            <scrollable layout="360px stretch" peeking="48">
                <lane orientation="vertical">
                    <label text="Egg groups" />
                    <lane *repeat={EggGroups} orientation="vertical">
                        <button layout="stretch content"
                                text={ButtonText}
                                click=|SelectEggGroup()| />
                        <label text={MetaText} />
                    </lane>
                </lane>
            </scrollable>

            <scrollable layout="stretch stretch" peeking="48">
                <lane orientation="vertical">
                    <label text="Current group" />
                    <label text={SelectedEggGroup} />
                    <label text="Provider options" />
                    <lane *repeat={ProviderOptions} orientation="vertical">
                        <button layout="stretch content"
                                text={ButtonText}
                                click=|SelectProvider()| />
                        <label text={DescriptionText} />
                    </lane>
                    <label text="Selected provider" />
                    <label text={SelectedProvider} />
                </lane>
            </scrollable>
        </lane>

        <lane layout="stretch content">
            <button text="Save" click=|Save()| />
            <button text="Discard" click=|Discard()| />
            <button text="Reload" click=|Reload()| />
            <button text="Reset" click=|ResetSelection()| />
        </lane>
    </lane>
</frame>
