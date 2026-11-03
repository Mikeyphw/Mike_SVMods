#!/usr/bin/env python3
"""Verify R06a actual Devtool machine receipt, not just source existence."""
from __future__ import annotations
import argparse, hashlib, json, re, sys
from pathlib import Path

class EvidenceError(ValueError): pass
SHA=re.compile(r'[0-9a-f]{40}\Z')

def _read_json(path:Path)->dict:
    if path.is_symlink() or not path.is_file():raise EvidenceError('Missing or symlink evidence: '+path.name)
    try: obj=json.loads(path.read_text(encoding='utf-8'))
    except (ValueError,UnicodeError) as exc:raise EvidenceError('Invalid JSON: '+path.name) from exc
    if not isinstance(obj,dict) or not obj:raise EvidenceError('Empty/nonobject JSON: '+path.name)
    return obj

def verify(evidence:Path,consumer_sha:str,devtool_sha:str)->dict:
    if not SHA.fullmatch(consumer_sha) or not SHA.fullmatch(devtool_sha):
        raise EvidenceError('SHA must be full lowercase Git revision')
    if evidence.is_symlink() or not evidence.is_dir():raise EvidenceError('Evidence folder missing/symlinked')
    plan=_read_json(evidence/'plan.json')
    preflight=_read_json(evidence/'preflight.json')
    invocation=_read_json(evidence/'invoke.json')
    if invocation.get('ok') is not True or invocation.get('exit_code')!=0:
        raise EvidenceError('Native Devtool invocation did not succeed')
    totals=invocation.get('totals')
    if not isinstance(totals,dict) or totals.get('failed')!=0 or totals.get('errors')!=0:
        raise EvidenceError('Native Devtool machine totals must be zero failures/errors')
    results=invocation.get('results')
    if not isinstance(results,list) or not results:
        raise EvidenceError('Native Devtool invoked zero jobs')
    wf=invocation.get('workflow')
    nodes=wf.get('nodes') if isinstance(wf,dict) else None
    if not isinstance(nodes,list) or not nodes:
        raise EvidenceError('Native Devtool machine lacks executable DAG nodes')
    sources=[n for n in nodes if isinstance(n,dict) and
            isinstance(n.get('job'),dict) and
            n['job'].get('target')=='svmods' and
            n['job'].get('local_name')=='ci-source']
    if len(sources)!=1:
        raise EvidenceError('Expected exactly one svmods.ci-source native DAG node')
    if sources[0].get('state') not in ('passed','success','completed','ok'):
        raise EvidenceError('Source node is not recorded as completed/passed')
    return {'schema_version':1,'status':'HOSTED_NATIVE_SOURCE_PASS',
            'consumer_sha':consumer_sha,'devtool_sha':devtool_sha,
            'node_count':len(nodes),'result_count':len(results),
            'plan_sha256':hashlib.sha256((evidence/'plan.json').read_bytes()).hexdigest(),
            'preflight_sha256':hashlib.sha256((evidence/'preflight.json').read_bytes()).hexdigest(),
            'invoke_sha256':hashlib.sha256((evidence/'invoke.json').read_bytes()).hexdigest(),
            'net6_test_qualification':'SEPARATE_BASELINE_CI',
            'release_qualified':False}

def main()->int:
    p=argparse.ArgumentParser();p.add_argument('--evidence',type=Path,required=True)
    p.add_argument('--consumer-sha',required=True);p.add_argument('--devtool-sha',required=True)
    p.add_argument('--receipt',type=Path,required=True);a=p.parse_args()
    try:
        report=verify(a.evidence,a.consumer_sha,a.devtool_sha)
        if a.receipt.resolve()!= (a.evidence/'receipt.json').resolve():
            raise EvidenceError('Receipt must remain in evidence directory')
        a.receipt.write_text(json.dumps(report,sort_keys=True,indent=2)+'\n',encoding='utf-8')
    except (EvidenceError,OSError) as exc:
        print(json.dumps({'status':'FAIL','error':str(exc)},sort_keys=True),file=sys.stderr)
        return 1
    print(json.dumps(report,sort_keys=True));return 0
if __name__=='__main__':sys.exit(main())
