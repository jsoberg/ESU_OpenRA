# ========================================================
# Note: Geared to work only on Windows machines for now.
# ========================================================
import sys
import subprocess
import os


BASE_PROCESS_ARGS = ['OpenRA.Game', 'Launch.Ai=ESU AI', 'Launch.MapName=Forest Path',
                     'Launch.AiSpawnPoint=0', 'Launch.AiFaction=russia', 'Launch.LogPrepend=%d']
END_GAME_FITNESS_LOG_DOC_FILEPATH = 'OpenRA\\Logs\\%d_end_game_fitness.log'
WIN_SEARCH_PHRASE = 'WIN'


def winlog_exists(process_num):
    doc_path = os.path.expanduser('~\Documents')
    logpath = os.path.join(doc_path, (END_GAME_FITNESS_LOG_DOC_FILEPATH % process_num))
    print('Checking ' + logpath + "...")

    if not os.path.isfile(logpath):
        print('FAIL: %s does not exist' % logpath)
        return False

    with open(logpath, "r") as logfile:
        lines = logfile.readlines()
    for line in reversed(lines):
        if WIN_SEARCH_PHRASE in line:
            print('SUCCESS: Definitive win log for %s exists' % logpath)
            return True

    print('FAIL: Could not find definitive win log in %s' % logpath)
    return False


def run_process(process_num):
    print('Running Process %d' % process_num)
    process_args = list(BASE_PROCESS_ARGS)
    process_args[-1] = process_args[-1] % process_num
    subprocess.call(process_args, shell=True, cwd='..\\')


def main(n):
    num_fails = 0
    for i in range(n):
        run_process(i)
        if not winlog_exists(i):
            num_fails += 1
    print('%d failures out of %d runs.' % (num_fails, n))


args = sys.argv[1:]
if len(args) != 1:
    print('Must provide number of processes to run')
    sys.exit()
main(int(args[0]))


