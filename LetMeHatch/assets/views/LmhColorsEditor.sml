<!--
Contract:
- MenuId: lmh.colors.editor
- RootViewModel: LmhColorsEditorViewModel
- Properties:
  - Title
  - PaletteGroups
  - SelectedPaletteGroup
  - PreviewLabel
  - PreviewStatusText
  - OverrideRows
  - CanSave
  - CanDiscard
- Methods:
  - OpenPaletteGroup()
  - SelectOverrideRow()
  - ResetOverride()
  - Save()
  - Discard()
  - Reload()
-->
<frame layout="1160px 760px"
       background={@Mods/StardewUI/Sprites/MenuBackground}
       border={@Mods/StardewUI/Sprites/MenuBorder}
       border-thickness="36, 36, 40, 36">
    <lane layout="stretch stretch" orientation="vertical" padding="24">
        <lane layout="stretch content" orientation="vertical">
            <banner text={Title} />
            <label text={PreviewLabel} />
            <label text={PreviewStatusText} />
        </lane>

        <lane layout="stretch stretch">
            <scrollable layout="340px stretch" peeking="48">
                <lane orientation="vertical">
                    <label text="Palette groups" />
                    <lane *repeat={PaletteGroups} orientation="vertical">
                        <button layout="stretch content"
                                text={ButtonText}
                                click=|OpenPaletteGroup()| />
                        <label text={MetaText} />
                    </lane>
                </lane>
            </scrollable>

            <scrollable layout="stretch stretch" peeking="48">
                <lane orientation="vertical">
                    <label text="Selected palette group" />
                    <label text={SelectedPaletteGroup} />
                    <label text="Override rows" />
                    <lane *repeat={OverrideRows} orientation="vertical">
                        <button layout="stretch content"
                                text={ButtonText}
                                click=|SelectOverrideRow()| />
                        <label text={DescriptionText} />
                        <button text="Reset override" click=|ResetOverride()| />
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
