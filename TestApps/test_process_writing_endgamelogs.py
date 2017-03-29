# ========================================================
# Note: Geared to work only on Windows machines for now.
# ========================================================
import time
import sys
import subprocess
import os
from threading import Lock
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime


BASE_PROCESS_ARGS = ['OpenRA.Game', 'Launch.Ai=ESU AI', 'Launch.MapName=Forest Path',
                     'Launch.AiSpawnPoint=0', 'Launch.AiFaction=russia', 'Launch.LogPrepend=%siter-%d_process-%d']
END_GAME_FITNESS_LOG_DOC_FILEPATH = 'OpenRA\\Logs\\%siter-%d_process-%d_end_game_fitness.log'
WIN_SEARCH_PHRASE = 'WIN'
OUTPUT_DIR = 'output'

_log_prepend = ""
_output_log_lock = Lock()


def print_elapsedtime(prepend, start_time, end_time):
    m, s = divmod(end_time - start_time, 60)
    h, m = divmod(m, 60)
    print_all("%s: %d:%02d:%02d" % (prepend, h, m, s))


def print_all(log):
    print(log)
    with _output_log_lock:
        if not os.path.exists(OUTPUT_DIR):
            os.makedirs(OUTPUT_DIR)
        global _log_prepend
        with open('output\\%stest_process_writing_endgamelogs_output.log' % _log_prepend, 'a') as output:
            output.write(log + "\n")


def winlog_exists(iteration_num, process_num):
    doc_path = os.path.expanduser('~\Documents')
    global _log_prepend
    logpath = os.path.join(doc_path, (END_GAME_FITNESS_LOG_DOC_FILEPATH % (_log_prepend, iteration_num, process_num)))
    print_all('Checking ' + logpath + "...")

    if not os.path.isfile(logpath):
        print_all('FAIL: %s does not exist' % logpath)
        return False

    with open(logpath, "r") as logfile:
        lines = logfile.readlines()
    for line in reversed(lines):
        if WIN_SEARCH_PHRASE in line:
            print_all('SUCCESS: Definitive win log for %s exists' % logpath)
            return True

    print_all('FAIL: Could not find definitive win log in %s' % logpath)
    return False


def run_process(iteration_num, process_num):
    print_all('Running Iteration %d, Process %d at %s' % (iteration_num, process_num,
                                                          datetime.now().strftime('%Y-%m-%d %H:%M:%S')))
    process_args = list(BASE_PROCESS_ARGS)
    global _log_prepend
    process_args[-1] = process_args[-1] % (_log_prepend, iteration_num, process_num)
    start_time = time.time()
    with open('output\\%siter%d_process%d_console.log' % (_log_prepend, iteration_num, process_num), 'w') as console:
        with open('output\\%siter%d_process%d_errors.log' % (_log_prepend, iteration_num, process_num), 'w') as error:
            subprocess.call(process_args, shell=True, cwd='..\\', stdout=console, stderr=error)

    end_time = time.time()
    print_all('Iteration %d, Process %d completed at %s' % (iteration_num, process_num,
                                                          datetime.now().strftime('%Y-%m-%d %H:%M:%S')))
    print_elapsedtime('Runtime for Iteration %d, Process %d' % (iteration_num, process_num), start_time, end_time)


def handle_process(iteration_num, process_num):
    run_process(iteration_num, process_num)
    return winlog_exists(iteration_num, process_num)


def perform_iterations(iters, num_procs):
    num_failures = 0
    for i in range(iters):
        start_time = time.time()
        futures = []
        with ThreadPoolExecutor(max_workers=20) as executor:
            # Spawn processes.
            for j in range(num_procs):
                future = executor.submit(handle_process, i, j)
                futures.append(future)
            # Wait for spawned processes.
            for current_future in futures:
                if not current_future.result():
                    num_failures += 1

        end_time = time.time()
        print_elapsedtime('Runtime for Iteration %d' % i, start_time, end_time)
    return num_failures


def main():
    args = sys.argv[1:]
    if len(args) < 1:
        print_all('Must provide number of iterations to run')
        sys.exit()

    global _log_prepend
    _log_prepend = datetime.now().strftime('%Y-%m-%d_%H-%M-%S__')

    iterations = int(args[0])
    try:
        num_processes = int(args[1])
    except IndexError:
        num_processes = 1
    print_all('Starting test with %d iterations of %d processes each' % (iterations, num_processes))

    start_time = time.time()
    num_failures = perform_iterations(iterations, num_processes)
    print_all('%d failures out of %d runs.' % (num_failures, iterations * num_processes))
    end_time = time.time()
    print_elapsedtime('Total Runtime', start_time, end_time)


main()
