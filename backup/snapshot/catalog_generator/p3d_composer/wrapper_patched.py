
from importlib import reload
import os
from varmain.custom import *    # type: ignore

import primitives as prim
import hot_reload

_HERE = os.path.normcase(os.path.dirname(os.path.abspath(__file__)))


def _composer_mode_active():
    return os.path.isfile(os.path.join(_HERE, ".p3d_composer_mode"))


#(testacpscript "wrapper")
@activate()  # type: ignore
def wrapper(s, D=80, K=1, **kw):
    if _composer_mode_active():
        print("P3D Composer: wrapper entry D=%s K=%s" % (D, K))
        reload(hot_reload)
        return hot_reload.hot_reload(s)

    reload(hot_reload)
    return hot_reload.hot_reload(s)
