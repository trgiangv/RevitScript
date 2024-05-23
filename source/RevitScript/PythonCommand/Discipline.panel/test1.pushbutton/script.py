import clr

STORE_PATH = r"C:\Users\trgiangvu\Downloads\WpfApp1"

# compile ironpython script
clr.CompileModules(STORE_PATH + r"\script.py")