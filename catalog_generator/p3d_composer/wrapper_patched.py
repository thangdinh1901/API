
from importlib import reload
from varmain.custom import *    # type: ignore

import primitives as prim
import hot_reload


#(testacpscript "wrapper")
@activate()  # type: ignore
def wrapper(s, D=80, K=1, S="", **kw):
    try:
        # SDK R2: reload hot_reload on every call so geometry edits apply without CAD restart.
        reload(hot_reload)
        return hot_reload.hot_reload(s, S=S, D=D, K=K, **kw)
    except Exception as e:
        print("P3D Composer: wrapper error: %s" % e)
        import traceback
        traceback.print_exc()
        return prim.Box(s, 50, 50, 50)
