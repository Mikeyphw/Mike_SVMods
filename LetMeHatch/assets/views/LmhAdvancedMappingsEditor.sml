<!--
Contract:
- MenuId: lmh.advancedMappings.editor
- RootViewModel: LmhItemMappingsEditorViewModel
- Properties:
  - Title
  - MappingGroups
  - SelectedGroup
  - SelectedMapping
  - HasSelectedMapping
  - ComparisonText
  - CanSave
  - CanDiscard
- Methods:
  - OpenGroup()
  - OpenMapping()
  - ResetMapping()
  - Save()
  - Discard()
  - Reload()
-->
<frame layout="1180px 760px"
       background={@Mods/StardewUI/Sprites/MenuBackground}
       border={@Mods/StardewUI/Sprites/MenuBorder}
       border-thickness="36, 36, 40, 36">
    <lane layout="stretch stretch" orientation="vertical" padding="24">
        <lane layout="stretch content" orientation="vertical">
            <banner text={Title} />
            <label text={ComparisonText} />
        </lane>

        <lane layout="stretch stretch">
            <scrollable layout="340px stretch" peeking="48">
                <lane orientation="vertical">
                    <label text="Mapping groups" />
                    <lane *repeat={MappingGroups} orientation="vertical">
                        <button layout="stretch content"
                                text={ButtonText}
                                click=|OpenGroup()| />
                        <label text={MetaText} />
                    </lane>
                </lane>
            </scrollable>

            <scrollable layout="stretch stretch" peeking="48">
                <lane orientation="vertical">
                    <label text="Selected group" />
                    <label text={SelectedGroup} />
                    <lane *if={HasSelectedMapping} orientation="vertical">
                        <label text="Selected mapping" />
                        <label text={SelectedMapping} />
                        <label text={ComparisonText} />
                        <button text="Reset mapping" click=|ResetMapping()| />
                    </lane>
                    <label text="Group rows are supplied by the future backing viewmodel." />
                    <lane *repeat={MappingGroups} orientation="vertical">
                        <button layout="stretch content"
                                text={OpenText}
                                click=|OpenMapping()| />
                        <label text={StateText} />
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
