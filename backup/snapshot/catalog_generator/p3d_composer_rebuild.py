"""Scene-graph rebuild entry for Plant 3D testacpscript (multi-part / primitives).

Uses hot_reload composer path. Separate from wrapper.xml (D-only) so catalog
assemblies validate correctly.
"""
from importlib import reload

from varmain.custom import *  # type: ignore

import hot_reload


#(testacpscript "p3d_composer_rebuild")
@activate()  # type: ignore
def p3d_composer_rebuild(s, K=1, **kw):
    print("P3D Composer: p3d_composer_rebuild K=%s" % K)
    reload(hot_reload)
    return hot_reload.hot_reload(s)
