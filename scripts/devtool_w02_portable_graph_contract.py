#!/usr/bin/env python3
"""M02: source-only native Linux+Windows .NET 12-node qualification.

The existing independently passed DN03-FINAL 58-test workflow is preserved.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import re

from devtool.actions_graph_export import compile_graph, workflow_yaml

HOSTS=('ubuntu-24.04-x64','windows-2022-x64')
NAMES=('restore_pure','build_pure','test_pure',
       'restore_animal','build_animal','test_animal')
OPS=('restore','build','test','restore','build','test')
OUTS={2:['dist/pure/results.trx'],5:['dist/animal/results.trx']}


def check(root: Path, devtool_sha: str) -> dict:
    if not re.fullmatch(r'[0-9a-f]{40}',devtool_sha):
        raise ValueError('W02 requires a full immutable 40-character Devtool commit')
    graph=compile_graph(root,target='dn03_r5',workflow='native_r6',
                        devtool_sha=devtool_sha,host_profiles=','.join(HOSTS),cache_locks=False)
    jobs=graph['jobs']
    if len(jobs)!=12 or list(graph.get('host_provider_plan_sha256',{}))!=list(HOSTS):
        raise ValueError('W02 expected separately reviewed 12-node two-host matrix')
    details=[]
    for idx,profile in enumerate(HOSTS):
        cohort=jobs[idx*6:(idx+1)*6]
        for index,job in enumerate(cohort):
            expected_needs=[] if index==0 else [cohort[index-1]['id']]
            if (job['profile_id']!=profile or job['job']!=NAMES[index] or
                job['dotnet_command']!=OPS[index] or job['provider']!='dotnet' or
                job['needs']!=expected_needs or job['outputs']!=OUTS.get(index,[]) or
                job['dotnet_legacy_runtime']!='6.0.36'):
                raise ValueError(f'W02 native graph contract drift in {profile}/{index+1}')
        details.append({'host':profile,'nodes':len(cohort),'test_projects':2,
                        'expected_tests':29,'runtime':'6.0.36'})
    yaml=workflow_yaml(graph)
    if 'runs-on: windows-2022' not in yaml or 'runs-on: ubuntu-24.04' not in yaml:
        raise ValueError('W02 native YAML did not preserve both reviewed hosts')
    return {'schema_version':1,'kind':'devtool.w02-m02-portable-dotnet-source-graph',
            'qualification':'SOURCE_ONLY','status':'PASS','devtool_sha':devtool_sha,
            'graph_sha256':graph['graph_sha256'],'consumer_commit':graph['consumer_commit'],
            'nodes':len(jobs),'hosts':details,'expected_tests':58,
            'host_provider_plan_sha256':graph['host_provider_plan_sha256'],
            'hosted_execution_verified':False,'remote_authenticated':False,'release_qualified':False}


def main() -> int:
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1])
    p.add_argument('--devtool-sha',required=True)
    p.add_argument('--output',type=Path)
    args=p.parse_args()
    try:
        result=check(args.root.resolve(),args.devtool_sha)
        body=json.dumps(result,sort_keys=True,indent=2)+'\n'
        if args.output:
            if args.output.exists() or args.output.is_symlink():
                raise ValueError('W02 refuses existing evidence path')
            args.output.write_text(body,encoding='utf-8')
        print(body,end='')
        return 0
    except (ValueError,OSError,KeyError) as exc:
        print('W02_M02_SOURCE_REFUSED: '+str(exc))
        return 2

if __name__=='__main__':
    raise SystemExit(main())
