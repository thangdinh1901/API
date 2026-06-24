"""Plant 3D hot reload — based on SPDS Plant3D Python SDK R2.

SDK pattern: reload target module + primitives on every wrapper call.
Extended: purge CustomScripts sys.modules cache, composer scene_builder preview,
and flat CUST_*.py catalog scripts (Test Catalog via S=CUST_…).
"""
from importlib import reload
import importlib
import os
import sys

_HERE = os.path.normcase(os.path.dirname(os.path.abspath(__file__)))

_KEEP = {"wrapper", "hot_reload"}


def _fresh_module(name):
    """Import or reload after purge (reload() fails if module was removed from sys.modules)."""
    if name in sys.modules:
        return importlib.reload(sys.modules[name])
    return importlib.import_module(name)


def _purge_local_modules():
    """Drop cached modules under CustomScripts so the next import reads .py from disk."""
    for name, mod in list(sys.modules.items()):
        if name in _KEEP:
            continue
        f = getattr(mod, "__file__", None)
        if not f:
            continue
        if os.path.normcase(os.path.abspath(f)).startswith(_HERE + os.sep):
            del sys.modules[name]


def _run_composer_scene(s):
    flag = os.path.join(_HERE, ".p3d_composer_mode")
    lib = os.path.join(_HERE, "p3d_composer")
    scene_path = os.path.join(_HERE, ".active_scene.json")
    if not (os.path.isfile(flag) and os.path.isfile(scene_path)):
        return None

    if lib not in sys.path:
        sys.path.insert(0, lib)

    sb = _fresh_module("scene_builder")
    print("P3D Composer: scene_builder from %s" % scene_path)
    scene = sb.load_scene(scene_path)
    if not (scene.get("parts") or []):
        raise RuntimeError("scene JSON has no parts — insert a catalog part first")
    out = sb.build_combined_scene(scene, s)
    print("P3D Composer: live Python build OK (%d part(s))" % len(scene.get("parts") or []))
    return out.set_color(120)


def _run_catalog_script(s, script_name, **kw):
    """Reload flat CUST_*.py and invoke its @activate entry (SDK reload pattern)."""
    _purge_local_modules()
    _fresh_module("primitives")

    mod = _fresh_module(script_name)
    fn = getattr(mod, script_name)
    print("P3D Composer: hot_reload catalog %s" % script_name)
    return fn(s, **kw)


def hot_reload(s, S="", D=80, K=1, **kw):
    _ = D, K
    script_name = (S or kw.get("S") or "").strip()
    meta = frozenset({"S", "D", "K", "P"})
    catalog_kw = {k: v for k, v in kw.items() if k not in meta}

    if script_name:
        return _run_catalog_script(s, script_name, **catalog_kw)

    _purge_local_modules()

    composer_out = _run_composer_scene(s)
    if composer_out is not None:
        return composer_out

    try:
        _fresh_module("primitives")
        ts = _fresh_module("test_script")
        return ts.test_script(s)
    except ImportError:
        pass

    raise RuntimeError(
        "P3D Composer: no catalog script (S=CUST_…) and no composer scene — "
        "insert a part in Composer or Deploy Catalog first."
    )
