#!/usr/bin/env python3
"""Source-only DN03-R5 acceptance for Mike_SVMods; never launches dotnet."""
from pathlib import Path
import json
import sys
import tomllib
import subprocess
import xml.etree.ElementTree as ET


def check(root: Path) -> None:
    cfg=tomllib.loads((root/'.devtool.toml').read_text(encoding='utf-8'))
    target=cfg['targets']['dn03_r5']
    assert target['runner']=='dotnet' and target['root']=='.'
    nodes=target['workflows']['native']
    expected=(('restore_pure',[]),('build_pure',['restore_pure']),('test_pure',['build_pure']),
              ('restore_animal',['test_pure']),('build_animal',['restore_animal']),
              ('test_animal',['build_animal']))
    assert [(v['id'],v['depends_on']) for v in nodes]==list(expected)
    jobs=target['jobs']
    assert set(jobs)=={node for node,_ in expected}
    tracked_result=subprocess.run(['git','-C',str(root),'ls-files','-z'],
                                  capture_output=True,check=True)
    tracked={p.decode('utf8') for p in tracked_result.stdout.split(b'\0') if p}
    owned_outputs={p for j in jobs.values() for p in j['outputs']}
    for ident,_ in expected:
        job=jobs[ident]
        assert job['inputs'] and len(job['inputs'])==len(set(job['inputs']))
        assert job['inputs']==sorted(job['inputs'])
        for item in job['inputs']:
            assert item in tracked and item not in owned_outputs
            assert not any(c in item for c in '*?[]')
        project_sources=('Tests/RoosterControl.PureTests/' if 'pure' in ident
                         else 'Tests/EggsInWild.AnimalNotes.Tests/')
        assert any(p.startswith(project_sources) and p.endswith('.csproj') for p in job['inputs'])
        assert job['runner']=='command'
        assert job['metadata']['ci_dotnet_allow_eol'] is True
        assert job['metadata']['ci_dotnet_selected_project_only'] is True
        project=('Tests/RoosterControl.PureTests/RoosterControl.PureTests.csproj' if 'pure' in ident
                 else 'Tests/EggsInWild.AnimalNotes.Tests/EggsInWild.AnimalNotes.Tests.csproj')
        command=ident.split('_')[0]
        assert job['command'][:3]==['dotnet',command,project]
        if command=='restore':
            assert job['command'][3:]==['--locked-mode'] and job['outputs']==[]
        elif command=='build':
            assert '--no-restore' in job['command'] and job['outputs']==[]
        else:
            assert '--no-restore' in job['command']
            assert 'trx;LogFileName=results.trx' in job['command']
            assert len(job['outputs'])==1 and job['outputs'][0].startswith('dist/')
    sdk=json.loads((root/'global.json').read_text(encoding='utf-8'))['sdk']
    assert sdk['version']=='10.0.100' and sdk['allowPrerelease'] is False
    for folder in ('Tests/RoosterControl.PureTests','Tests/EggsInWild.AnimalNotes.Tests','RoosterControl.Pure'):
        props=root/folder/'Directory.Build.props'
        doc=ET.parse(props)
        assert doc.getroot().tag=='Project'
        assert doc.getroot().find('PropertyGroup/TargetFramework').text=='net6.0'
    for name in ('Tests/RoosterControl.PureTests/packages.lock.json','Tests/EggsInWild.AnimalNotes.Tests/packages.lock.json'):
        assert 'net6.0' in json.loads((root/name).read_text(encoding='utf-8'))['dependencies']
    assert json.loads((root/'RoosterControl.Pure/packages.lock.json').read_text())['dependencies']['net6.0']=={}

if __name__=='__main__':
    check(Path(sys.argv[1]).resolve() if len(sys.argv)>1 else Path.cwd())
    print('DN03_R5_CONSUMER_SOURCE_PASS')
