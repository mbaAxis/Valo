import xlwings
import os

class Excel_helpers:

    def get_file_paths(folder_path):
        file_paths = []
        files = os.listdir(folder_path)
        for file_name in files:
            file_path = os.path.join(folder_path, file_name)
            if os.path.isfile(file_path):
                file_paths.append(file_path)
        return file_paths

    def empty_folder(folder_path):
        files = os.listdir(folder_path)
        for file_name in files:
            file_path = os.path.join(folder_path, file_name)
            if os.path.isfile(file_path):
                os.remove(file_path)

    def insert_plot_to_excel(sheet, image_path, cell, plot_name):
        sheet.pictures.add(image_path, name=plot_name, update=True,
                           left=sheet.range(cell).left, top=sheet.range(cell).top, scale=0.7)

