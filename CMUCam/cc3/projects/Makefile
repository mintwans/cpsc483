SUBDIRS=cmucam2 hello-world jpeg-cam benchmark edge-follow polly \
	led-test png-grab spoonBot ppm-grab eeprom-test security-cam relive

# Build rules
all:
	@list='$(SUBDIRS)'; set -e;  for subdir in $$list; do \
	  $(MAKE) -C $$subdir; \
	done

clean:
	@list='$(SUBDIRS)'; set -e;  for subdir in $$list; do \
	  $(MAKE) -C $$subdir clean; \
	done


.PHONY: all clean
