<frame layout="1200px 760px"
       background={@Mods/StardewUI/Sprites/MenuBackground}
       border={@Mods/StardewUI/Sprites/MenuBorder}
       border-thickness="36, 36, 40, 36">
    <lane layout="stretch stretch" orientation="vertical" padding="24" item-spacing="16">
        <banner text="Wild Animals Editor" />
        <label text={StatusText} />
        <lane layout="stretch content" item-spacing="12">
            <button text="Save" click=|SaveCommand.Execute()| />
            <button text="Discard" click=|DiscardCommand.Execute()| />
            <button text="Reload" click=|ReloadCommand.Execute()| />
        </lane>
        <lane layout="stretch stretch" item-spacing="24">
            <scrollable layout="340px stretch" peeking="48">
                <lane orientation="vertical" item-spacing="12">
                    <expander *repeat={AnimalGroups} layout="stretch content" is-expanded="true">
                        <label *outlet="header" text={Title} />
                        <lane orientation="vertical" margin="16,8,0,8" item-spacing="4">
                            <button *repeat={Animals}
                                    layout="stretch content"
                                    text={Label}
                                    click=|Open.Execute()| />
                        </lane>
                    </expander>
                </lane>
            </scrollable>
            <scrollable layout="stretch stretch" peeking="64">
                <lane *if={HasSelectedAnimal} orientation="vertical" item-spacing="16">
                    <lane *context={SelectedAnimal} orientation="vertical" item-spacing="12">
                        <label font="dialogue" text={Key} />
                        <checkbox label-text="Enabled" is-checked={<>Enabled} />
                        <checkbox label-text="Large egg" is-checked={<>LargeEgg} />
                        <checkbox label-text="Magic egg" is-checked={<>IsMagic} />
                        <lane orientation="vertical" item-spacing="4">
                            <label text="Price" />
                            <textinput layout="220px 54px" text={<>PriceText} />
                        </lane>
                        <label text="Location types" />
                        <checkbox *repeat={LocationTypes}
                                  label-text={Label}
                                  is-checked={<>Allowed} />
                        <label text="ExtraModAnimalKeys" />
                        <lane *repeat={ModGroups} orientation="vertical" item-spacing="4">
                            <label text={ModId} />
                            <label text={BaseKeysText} />
                            <textinput layout="stretch 54px" text={<>KeysText} />
                        </lane>
                        <lane orientation="vertical" item-spacing="4">
                            <label text="Add custom mod group" />
                            <lane item-spacing="8">
                                <textinput layout="stretch 54px" text={<>NewModGroupId} />
                                <button text="Add" click=|AddModGroupCommand.Execute()| />
                            </lane>
                        </lane>
                        <label text={ModGroupSummary} />
                    </lane>
                </lane>
            </scrollable>
        </lane>
    </lane>
</frame>
