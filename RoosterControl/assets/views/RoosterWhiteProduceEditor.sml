<frame layout="850px 500px"
       background={@Mods/StardewUI/Sprites/MenuBackground}
       border={@Mods/StardewUI/Sprites/MenuBorder}
       border-thickness="36, 36, 40, 36"
       padding="32,16,32,24">
    <lane layout="stretch stretch" orientation="vertical">
        <banner text={Title} />
        <scrollable layout="stretch stretch">
            <lane layout="stretch content" orientation="vertical">
                <frame *repeat={:Slots}
                       layout="stretch content"
                       margin="0,0,0,8"
                       background={@Mods/StardewUI/Sprites/ControlBorder}
                       padding="16,12,16,12">
                    <lane layout="stretch content" orientation="vertical">
                        <label font="dialogue" text={:SlotTitle} margin="0,0,0,8" />

                        <lane layout="stretch content" orientation="horizontal" margin="0,0,0,8">
                            <label layout="200px content" text={#config.produceEntry.choice.name} />
                            <dropdown layout="stretch content"
                                      option-min-width="300"
                                      options={^Choices}
                                      selected-option={<>Choice} />
                        </lane>

                        <lane *if={IsCustom} layout="stretch content" orientation="horizontal" margin="0,0,0,8">
                            <label layout="200px content" text={#config.produceEntry.customItemName.name} />
                            <textinput layout="stretch content" text={<>CustomItemName} />
                        </lane>

                        <lane layout="stretch content" orientation="horizontal">
                            <label layout="200px content" text={#config.produceEntry.probability.name} />
                            <slider track-width="200"
                                    min="0"
                                    max="100"
                                    interval="1"
                                    value-format={^FormatProbability}
                                    value={<>Probability} />
                        </lane>
                    </lane>
                </frame>
            </lane>
        </scrollable>
    </lane>
</frame>
