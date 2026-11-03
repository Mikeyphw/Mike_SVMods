#!/usr/bin/env python3
"""R06a fail-closed source policy; actual hosted execution is a separate gate."""
from __future__ import annotations
import json, re, sys, tomllib
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
DEVTOOL_SHA='b164463db51e2cffd12f753043b9e55a08cc43ae'
WF='.github/workflows/r06a-native-devtool-source.yml'
COMMAND='python3 scripts/check_r06_source_contract.py'

def check_source(workflow:str,ci:str,config:dict)->list[str]:
    problems=[]
    def require(pattern:str,label:str):
        if not re.search(pattern,workflow,re.MULTILINE|re.DOTALL):problems.append(label)
    require(r'^on:\s*\n  workflow_dispatch:\s*$', 'R06a must be manually dispatched')
    require(r'^permissions:\s*\n  contents: read\s*$', 'R06a requires read-only permissions')
    require(r'^    runs-on: ubuntu-24\.04\s*$', 'R06a requires reviewed hosted Ubuntu')
    require(r'^      DEVTOOL_SOURCE_SHA: '+DEVTOOL_SHA+r'\s*$', 'R06a Devtool source SHA drift')
    require(r'checkout@11d5960a326750d5838078e36cf38b85af677262', 'R06a checkout must remain pinned')
    require(r'upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02', 'R06a upload must remain pinned')
    for k in ('persist-credentials: false', '"$(git rev-parse HEAD)" = "$GITHUB_SHA"',
              'checkout --detach "$DEVTOOL_SOURCE_SHA"',
              '"$(git -C "$RUNNER_TEMP/devtool-source" rev-parse HEAD)" = "$DEVTOOL_SOURCE_SHA"',
              'plan ci-preflight --target svmods --json',
              'preflight --target svmods --workflow ci-preflight --json',
              'invoke --target svmods ci-preflight', '--machine-file "$RUNNER_TEMP/r06-evidence/invoke.json"',
              'verify_r06_native_evidence.py', 'if: always()', 'if-no-files-found: error',
              'r06a-native-devtool-${{ github.run_id }}-${{ github.run_attempt }}'):
        if k not in workflow: problems.append('R06a missing required control: '+k)
    if re.search(r'(?m)^\s*continue-on-error:\s*true\s*$',workflow):
        problems.append('R06a must not ignore any failure')
    if re.search(r'^\s*\-?\s*run:\s*dotnet\b|^\s*dotnet (?:restore|build|test|pack|publish)\b',workflow):
        problems.append('R06a must not execute .NET; source-only workflow')
    if re.search(r'(?m)^    env:\s*\n(?:(?!^    steps:).)*\$\{\{ runner\.',workflow,re.DOTALL):
        problems.append('Job-level runner context is forbidden')
    if not re.search(r'^\s+run: '+re.escape(COMMAND)+r'\s*$',ci,re.MULTILINE):
        problems.append('Baseline CI must enforce the R06a source contract')
    target=config.get('targets',{}).get('svmods',{})
    steps=target.get('workflows',{}).get('ci-preflight')
    job=target.get('jobs',{}).get('ci-source',{})
    if not isinstance(steps,list) or len(steps)!=1 or steps[0].get('ref')!='job:ci-source':
        problems.append('R06a requires the existing svmods/ci-preflight source-only DAG')
    if job.get('runner')!='command' or job.get('command')!=['python3','scripts/devtool_actions_contract.py']:
        problems.append('R06a cannot invoke an unreviewed source job')
    if job.get('metadata',{}).get('read_only') is not True:
        problems.append('R06a source job must be read-only')
    return problems

def main()->int:
    try:
        issues=check_source((ROOT/WF).read_text(),(ROOT/'.github/workflows/ci.yml').read_text(),
                            tomllib.loads((ROOT/'.devtool.toml').read_text()))
    except (OSError,ValueError) as exc: issues=[str(exc)]
    print(json.dumps({'status':'FAIL' if issues else 'PASS','errors':issues,
        'hosted_r06a_execution':'NOT_RUN_BY_SOURCE_CHECK','hosted_dotnet_invocation':'NOT_IN_R06A'},sort_keys=True))
    return bool(issues)
if __name__=='__main__':sys.exit(main())
