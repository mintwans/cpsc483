#ifndef _RELIVE_H_
#define _RELIVE_H_

#include "jpeglib.h"

#define DLE	0x10
#define ETX	0x03

#define DONE			1
#define ERROR		2
#define IN_PROGRESS	3
#define STATE_DLE		4
#define START			5

int state;
bool face;
int which_halo;
FILE *memory;
FILE *gps_com;

/************************************************************************/

void setup_copernicus(void);
int compute_checksum(char* msg, int len);
char digit_to_char_hex(int digit);
void initialize(void);
int takePict(int picNum);
bool check_triggers( int delta_time );
void write_gpsdata(char data, int opt);
void write_metadata(void);
void get_gps_data(void);
//int receive_byte( char byte, char* data, int dLen );

static void capture_current_jpeg(FILE *f);
static void init_jpeg(void);
static void destroy_jpeg(void);

/************************************************************************/

static struct jpeg_compress_struct cinfo;
static struct jpeg_error_mgr jerr;
uint8_t *row;

/************************************************************************/

static void init_jpeg()
{
	cinfo.err = jpeg_std_error(&jerr);
	jpeg_create_compress(&cinfo);

	// parameters for jpeg image
	cinfo.image_width = cc3_g_pixbuf_frame.width;
	cinfo.image_height = cc3_g_pixbuf_frame.height;
	printf( "image width=%d image height=%d\n", cinfo.image_width, cinfo.image_height );
	cinfo.input_components = 3;
	// cinfo.in_color_space = JCS_YCbCr;
	cinfo.in_color_space = JCS_RGB;

	// set image quality, etc.
	jpeg_set_defaults(&cinfo);
	jpeg_set_quality(&cinfo, 80, true);

	// allocate memory for 1 row
	row = cc3_malloc_rows(1);
	if(row==NULL)
		printf( "Out of memory!\n" );
}

/************************************************************************/

static void capture_current_jpeg(FILE *f)
{
	JSAMPROW row_pointer[1];
	row_pointer[0] = row;

	// output is file
	jpeg_stdio_dest(&cinfo, f);

	// capture a frame to the FIFO
	cc3_pixbuf_load();

	// read and compress
	jpeg_start_compress(&cinfo, TRUE);
	while (cinfo.next_scanline < cinfo.image_height)
	{
		cc3_pixbuf_read_rows(row, 1);
		jpeg_write_scanlines(&cinfo, row_pointer, 1);
	}

	// finish
	jpeg_finish_compress(&cinfo);
}

/************************************************************************/

static void destroy_jpeg()
{
	jpeg_destroy_compress(&cinfo);
	free(row);
}

#endif
