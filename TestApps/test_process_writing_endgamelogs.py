# ========================================================
# Note: Geared to work only on Windows machines for now.
# ========================================================
import time
import sys
import subprocess
import os
from concurrent.futures import ThreadPoolExecutor


BASE_PROCESS_ARGS = ['OpenRA.Game', 'Launch.Ai=ESU AI', 'Launch.MapName=Forest Path',
                     'Launch.AiSpawnPoint=0', 'Launch.AiFaction=russia', 'Launch.LogPrepend=iter-%d_process-%d']
END_GAME_FITNESS_LOG_DOC_FILEPATH = 'OpenRA\\Logs\\iter-%d_process-%d_end_game_fitness.log'
WIN_SEARCH_PHRASE = 'WIN'


def winlog_exists(iteration_num, process_num):
    doc_path = os.path.expanduser('~\Documents')
    logpath = os.path.join(doc_path, (END_GAME_FITNESS_LOG_DOC_FILEPATH % (iteration_num, process_num)))
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


def run_process(iteration_num, process_num):
    print('Running Iteration %d, Process %d' % (iteration_num, process_num))
    process_args = list(BASE_PROCESS_ARGS)
    process_args[-1] = process_args[-1] % (iteration_num, process_num)
    subprocess.call(process_args, shell=True, cwd='..\\')


def handle_process(iteration_num, process_num):
    run_process(iteration_num, process_num)
    return winlog_exists(iteration_num, process_num)


def main(iters, num_procs):
    num_failures = 0
    for i in range(iters):
        futures = []
        with ThreadPoolExecutor(max_workers=10) as executor:
            # Spawn processes.
            for j in range(num_procs):
                future = executor.submit(handle_process, i, j)
                futures.append(future)

            # Wait for spawned processes.
            for current_future in futures:
                if not current_future.result():
                    num_failures += 1

    return num_failures


args = sys.argv[1:]
if len(args) < 1:
    print('Must provide number of iterations to run')
    sys.exit()

iterations = int(args[0])
try:
    num_processes = int(args[1])
except IndexError:
    num_processes = 1
print('Starting test with %d iterations of %d processes each' % (iterations, num_processes))

start_time = time.time()
num_failures = main(iterations, num_processes)
print('%d failures out of %d runs.' % (num_failures, iterations * num_processes))
end_time = time.time()
m, s = divmod(end_time - start_time, 60)
h, m = divmod(m, 60)
print("Total Runtime: %d:%02d:%02d" % (h, m, s))

