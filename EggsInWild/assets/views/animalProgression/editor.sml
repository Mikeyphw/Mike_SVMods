<!--
Contract:
- MenuId: animalProgressionEditor
- RootViewModel: AnimalProgressionEditorViewModel
- Properties:
  - Title
  - SearchText
  - SummaryText
  - Animals
  - SelectedAnimal
  - HasSelectedAnimal
  - CurrentModeText
  - DefaultModeText
  - EffectiveModeText
  - ModeOptions
  - CanSave
  - CanDiscard
- Methods:
  - OpenAnimal()
  - SelectMode()
  - Save()
  - Discard()
  - Reload()
-->
<frame layout="1180px 780px"
       background={@Mods/StardewUI/Sprites/MenuBackground}
       border={@Mods/StardewUI/Sprites/MenuBorder}
       border-thickness="36, 36, 40, 36">
    <lane layout="stretch stretch" orientation="vertical" padding="24">
        <lane layout="stretch content" orientation="vertical">
            <banner text={Title} />
            <label text={SummaryText} />
            <label text="Search" />
            <textinput layout="360px 54px" text={<>SearchText} />
        </lane>

        <lane layout="stretch stretch">
            <scrollable layout="360px stretch" peeking="48">
                <lane orientation="vertical">
                    <label text="Animals" />
                    <lane *repeat={Animals} orientation="vertical">
                        <button layout="stretch content"
                                text={ButtonText}
                                click=|OpenAnimal()| />
                        <label text={MetaText} />
                    </lane>
                </lane>
            </scrollable>

            <scrollable layout="stretch stretch" peeking="48">
                <lane *if={HasSelectedAnimal} orientation="vertical">
                    <label text="Selected animal" />
                    <label text={SelectedAnimal} />
                    <label text="Default mode" />
                    <label text={DefaultModeText} />
                    <label text="Current mode" />
                    <label text={CurrentModeText} />
                    <label text="Effective mode" />
                    <label text={EffectiveModeText} />
                    <label text="Mode options" />
                    <lane *repeat={ModeOptions} orientation="vertical">
                        <button layout="stretch content"
                                text={ButtonText}
                                click=|SelectMode()| />
                        <label text={DescriptionText} />
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
