
# Given API function
def IsBadVersion(x):
    global count
    count +=1

    if x >= firstBadVersion:
        return True
    else:
        return False

# Main function to divide the versions list into two chuncks and check each version
def RecursiveBinaryFunction(min, max):
    if IsBadVersion(max):
        global lastMax
        lastMax = max
        runRecursion = CheckRecursionStatus(min, max)
        if runRecursion == False:
            return 0
        RecursiveBinaryFunction(min, int((min + max) / 2))
    else:
        min = max
        max = lastMax
        runRecursion = CheckRecursionStatus(min, max)
        if runRecursion == False:
            return 0
        RecursiveBinaryFunction(min, int((min + max) / 2))

def CheckRecursionStatus(min, max):
    if max - min < 2:
        print('Version', max, ' is the first bad version')
        print('API calls : ', count)
        return False
    else:
        return True


# data member and function declaration here
min = 1
max = 10000000
lastMax = max
count = 0
firstBadVersion = 111111

RecursiveBinaryFunction(min, max)