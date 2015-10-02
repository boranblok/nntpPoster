#!/bin/sh

### BEGIN INIT INFO
# Provides:          nntpAutoPoster
# Required-Start:    $local_fs $syslog $remote_fs
# Required-Stop:     $local_fs $syslog $remote_fs
# Default-Start:     2 3 4 5
# Default-Stop:      0 1 6
# Short-Description: Start the nntp auto poster service
### END INIT INFO

### CHANGE the following:

APPROOT=<root folder of the application>
RUN_AS=<USER>




### DO NOT CHANGE ANYTHING BELOW

PATH=/usr/local/sbin:/usr/local/bin:/sbin:/bin:/usr/sbin:/usr/bin
SERVICEHOST=$(which mono-service)

PIDFILE=$APPROOT/nntpAutoposterWindowsService.pid
APPPATH=$APPROOT/nntpAutoPosterWindowsService.exe
DESC=nntpAutoPoster

case "$1" in
    start)
        if [ -f $PIDFILE ]; then
            kill -0 $(cat $PIDFILE)
            if [ $? -eq 0 ]; then
                echo 'Service already running' >&2
                return 1
            fi
        fi
        echo "Starting $DESC..." >&2
        su -c "$SERVICEHOST -d:$APPROOT -l:$PIDFILE -m:$DESC $APPPATH" - $RUN_AS
        echo "$DESC started" >&2
    ;;
    stop)
        if [ ! -f $PIDFILE ]; then
            echo "$DESC is not running" >&2
        else
            kill -0 $(cat $PIDFILE)
            if [ $? -eq 0 ]; then
                echo "Stopping $DESC..." >&2
                kill $(cat $PIDFILE)
                rm $PIDFILE
                echo "$DESC Stopped" >&2
            else
                echo "$DESC is not running" >&2
                return 1
            fi
        fi

    ;;
	cleanstop)
        if [ ! -f $PIDFILE ]; then
            echo "$DESC is not running" >&2
        else
            kill -0 $(cat $PIDFILE)
            if [ $? -eq 0 ]; then
                echo "Stopping $DESC..." >&2
                kill -USR1 $(cat $PIDFILE)
                rm $PIDFILE
                echo "$DESC Stopped" >&2
            else
                echo "$DESC is not running" >&2
                return 1
            fi
        fi

    ;;
	restart)
		if [ ! -f $PIDFILE ]; then
            echo "$DESC is not running" >&2
        else
            kill -0 $(cat $PIDFILE)
            if [ $? -eq 0 ]; then
                echo "Stopping $DESC..." >&2
                kill $(cat $PIDFILE)
                rm $PIDFILE
                echo "$DESC Stopped" >&2
				echo "Starting $DESC..." >&2
				su -c "$SERVICEHOST -d:$APPROOT -l:$PIDFILE -m:$DESC $APPPATH" - $RUN_AS
				echo "$DESC started" >&2
            else
                echo "$DESC is not running" >&2
                return 1
            fi
        fi

    ;;
    status)
        if [ ! -f $PIDFILE ]; then
            echo "$DESC is not running" >&2
        else
            kill -0 $(cat $PIDFILE)
            if [ $? -eq 0 ]; then
                echo "$DESC is running PID: $(cat $PIDFILE)" >&2
            else
                echo "$DESC is not running but PID file exists" >&2
            fi
        fi

    ;;
    *)
        echo "Usage: `basename $0` {start|stop|cleanstop|restart|status}" >&2
        exit 1
    ;;
esac

exit 0