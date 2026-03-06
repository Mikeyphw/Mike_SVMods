<!--
Contract:
- MenuId: rooster.females.editor
- RootViewModel: RoosterFemalesEditorViewModel
- Properties:
  - Title
  - SearchText
  - SummaryText
  - Females
  - SelectedFemale
  - HasSelectedFemale
  - AssignmentOptions
  - CanSave
  - CanDiscard
- Methods:
  - OpenFemale()
  - SelectAssignment()
  - Save()
  - Discard()
  - Reload()
-->
<frame layout="1120px 760px"
       background={@Mods/StardewUI/Sprites/MenuBackground}
       border={@Mods/StardewUI/Sprites/MenuBorder}
       border-thickness="36, 36, 40, 36">
    <lane layout="stretch stretch" orientation="vertical" padding="24">
        <lane layout="stretch content" orientation="vertical">
            <banner text={Title} />
            <label text={SummaryText} />
            <label text="Search text is owned by the future backing viewmodel." />
            <textinput layout="360px 54px" text={<>SearchText} />
        </lane>

        <lane layout="stretch stretch">
            <scrollable layout="360px stretch" peeking="48">
                <lane orientation="vertical">
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
                    <label text="Assignment options" />
                    <lane *repeat={AssignmentOptions} orientation="vertical">
                        <button layout="stretch content"
                                text={ButtonText}
                                click=|SelectAssignment()| />
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
